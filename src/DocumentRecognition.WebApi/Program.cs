
using DocumentRecognition.Application.Models;
using DocumentRecognition.Application.Services;
using DocumentRecognition.Infrastructure.Extensions;
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
                    [FromForm] IFormFile file,
                    [FromQuery] bool? includeDebugImage,
                    [FromServices] DocumentRecognitionService recognitionService) =>
                {
                    if (file is null || file.Length == 0)
                        return Results.BadRequest("PDF-файл не передан (ожидается multipart/form-data, поле \"file\").");

                    try
                    {
                        using var inputStream = file.OpenReadStream();

                        var result = await recognitionService.ProcessAsync(inputStream);
                        var response = DrawingRecognitionResultDto.FromResult(result, includeDebugImage == true);

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
                .Accepts<IFormFile>("multipart/form-data")
                .Produces<DrawingRecognitionResultDto>(StatusCodes.Status200OK)
                .WithName("RecognizeDrawing")
                .WithOpenApi(operation =>
                {
                    operation.Summary = "Распознаёт чертёж и возвращает основную надпись и технические требования";
                    operation.Description = "Принимает PDF в multipart/form-data (поле \"file\"). " +
                        "Параметр includeDebugImage=true добавляет отладочное изображение в base64.";
                    return operation;
                });

            app.Run();
        }
    }
}
