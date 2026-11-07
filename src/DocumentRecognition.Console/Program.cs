using DocumentRecognition.Application.Models;
using DocumentRecognition.Application.Services;
using DocumentRecognition.Core.Models.Results;
using DocumentRecognition.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocumentRecognition.Console
{
    internal static class Program
    {
        private const string Description = "Распознавание чертежей: основная надпись и технические требования";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static async Task<int> Main(string[] args)
        {
            var inputOption = new Option<FileInfo>("--input", "--filename")
            {
                Description = "Путь к PDF-файлу с чертежом",
                Required = true
            };
            inputOption.AcceptLegalFilePathsOnly();

            var jsonOption = new Option<bool>("--json")
            {
                Description = "Вывести результат в JSON (для интеграции со скриптами и сервисами)"
            };

            var outputOption = new Option<string?>("--output")
            {
                Description = "Записать результат в файл (по умолчанию — в стандартный вывод)"
            };

            var debugImageOption = new Option<string?>("--debug-image")
            {
                Description = "Сохранить отладочное изображение (найденные блоки) в PNG по указанному пути"
            };

            var dpiOption = new Option<int>("--dpi")
            {
                Description = "DPI при конвертации PDF (по умолчанию 200)"
            };

            var rootCommand = new RootCommand(Description);
            rootCommand.Add(inputOption);
            rootCommand.Add(jsonOption);
            rootCommand.Add(outputOption);
            rootCommand.Add(debugImageOption);
            rootCommand.Add(dpiOption);

            rootCommand.SetAction(async (parseResult, cancellationToken) =>
            {
                var inputFile = parseResult.GetValue(inputOption);
                if (inputFile is null || !inputFile.Exists)
                {
                    await System.Console.Error.WriteLineAsync($"Файл не найден: {inputFile?.FullName}");
                    return 2;
                }

                var dpi = parseResult.GetValue(dpiOption);
                if (dpi <= 0)
                    dpi = 200;

                try
                {
                    var result = await RecognizeAsync(inputFile, dpi);

                    var text = parseResult.GetValue(jsonOption)
                        ? JsonSerializer.Serialize(DrawingRecognitionResultDto.FromResult(result), JsonOptions)
                        : DrawingResultFormatter.ToText(inputFile.FullName, DrawingRecognitionResultDto.FromResult(result));

                    var outputPath = parseResult.GetValue(outputOption);
                    if (string.IsNullOrWhiteSpace(outputPath))
                    {
                        System.Console.WriteLine(text);
                    }
                    else
                    {
                        await File.WriteAllTextAsync(outputPath, text, cancellationToken);
                        await System.Console.Error.WriteLineAsync($"Результат записан в «{outputPath}»");
                    }

                    var debugImagePath = parseResult.GetValue(debugImageOption);
                    if (!string.IsNullOrWhiteSpace(debugImagePath) && result.DebugImage is { Length: > 0 })
                    {
                        await File.WriteAllBytesAsync(debugImagePath, result.DebugImage, cancellationToken);
                        await System.Console.Error.WriteLineAsync($"Отладочное изображение сохранено в «{debugImagePath}»");
                    }

                    return 0;
                }
                catch (Exception ex)
                {
                    await System.Console.Error.WriteLineAsync($"Ошибка распознавания: {ex.Message}");
                    return 1;
                }
            });

            return await rootCommand.Parse(args).InvokeAsync();
        }

        private static async Task<DrawingRecognitionResult> RecognizeAsync(FileInfo inputFile, int dpi)
        {
            using var serviceProvider = CreateServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var recognitionService = scope.ServiceProvider.GetRequiredService<DocumentRecognitionService>();

            using var stream = inputFile.OpenRead();

            return await recognitionService.ProcessAsync(stream, dpi);
        }

        private static ServiceProvider CreateServiceProvider()
        {
            // Пути к моделям берутся из OcrSettings по умолчанию,
            // поэтому конфигурация нужна здесь только как контракт DI.
            var configuration = new ConfigurationBuilder().Build();

            var services = new ServiceCollection();
            services.AddApplicationServices(configuration);

            return services.BuildServiceProvider();
        }
    }
}
