
using DocumentRecognition.Application.Models;
using DocumentRecognition.Application.Services;
using DocumentRecognition.Core.Models.Options;
using DocumentRecognition.Infrastructure.Extensions;
using DocumentRecognition.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;

namespace DocumentRecognition.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();
            builder.Services.AddApplicationServices(builder.Configuration);

            // Кириллица в JSON — как есть, без экранирования в \uXXXX.
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapPost("/recognize", async (
                    [FromForm] RecognizeDrawingRequest request,
                    [FromQuery] bool? includeDebugImage,
                    [FromQuery] bool? includeRegions,
                    [FromServices] DocumentRecognitionService recognitionService) =>
                {
                    if (request.File is null || request.File.Length == 0)
                        return Results.BadRequest("PDF-файл не передан (ожидается multipart/form-data, поле \"file\").");

                    if (request.Dpi is int requestedDpi && !DrawingRecognitionOptions.IsDpiInRange(requestedDpi))
                    {
                        return Results.BadRequest(
                            $"Некорректный dpi: {requestedDpi}. Допустимый диапазон: " +
                            $"{DrawingRecognitionOptions.MinDpi}–{DrawingRecognitionOptions.MaxDpi}.");
                    }

                    var options = new DrawingRecognitionOptions
                    {
                        Dpi = request.Dpi ?? DrawingRecognitionOptions.DefaultDpi,
                        CleanImage = request.CleanImage ?? true
                    };

                    try
                    {
                        using var inputStream = request.File.OpenReadStream();

                        var result = await recognitionService.ProcessAsync(inputStream, options);
                        var response = DrawingRecognitionResultDto.FromResult(result, includeDebugImage == true, includeRegions == true);

                        return Results.Ok(response);
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem(
                            detail: ex.Message,
                            statusCode: StatusCodes.Status500InternalServerError,
                            title: "Ошибка распознавания чертежа");
                    }
                })
                .DisableAntiforgery()
                .Accepts<RecognizeDrawingRequest>("multipart/form-data")
                .Produces<DrawingRecognitionResultDto>(StatusCodes.Status200OK)
                .WithName("RecognizeDrawing")
                .WithOpenApi(operation =>
                {
                    operation.Summary = "Распознаёт чертёж и возвращает основную надпись и технические требования";
                    operation.Description = "Принимает PDF в multipart/form-data: file (обязательно), " +
                        "dpi (200–900, по умолчанию 200), cleanImage (без отладочных рамок, по умолчанию true). " +
                        "Параметр includeDebugImage=true добавляет отладочное изображение в base64, " +
                        "includeRegions=true — координаты областей распознавания.";
                    return operation;
                });

            app.Run();
        }
    }
}
