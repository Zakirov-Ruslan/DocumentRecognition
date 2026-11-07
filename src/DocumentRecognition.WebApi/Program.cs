
using DocumentRecognition.Application.Services;
using DocumentRecognition.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;

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

            app.MapPost("/recognize", async (HttpRequest request, [FromServices] DocumentRecognitionService recognitionService) =>
            {
                if (!request.HasFormContentType)
                    return Results.BadRequest("Ожидается multipart/form-data с PDF-файлом.");

                var form = await request.ReadFormAsync();
                var file = form.Files.FirstOrDefault();

                if (file is null || file.Length == 0)
                    return Results.BadRequest("Файл не передан.");

                using (var inputStream = file.OpenReadStream())
                {
                    var result = await recognitionService.ProcessAsync(inputStream);

                    return Results.Ok(result);
                }
            })
            .WithName("Recognize")
            .WithOpenApi();

            app.Run();
        }
    }
}
