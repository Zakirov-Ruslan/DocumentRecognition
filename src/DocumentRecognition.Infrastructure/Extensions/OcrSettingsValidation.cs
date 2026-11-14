using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;

namespace DocumentRecognition.Infrastructure.Extensions
{
    /// <summary>
    /// Проверка наличия файлов ONNX-моделей и словаря.
    /// </summary>
    /// <remarks>
    /// Вызывается при старте приложения (WebApi, через <see cref="OcrSettingsValidator"/>)
    /// и при изменении настроек в UI, чтобы приложение не падало уже во время распознавания.
    /// </remarks>
    public static class OcrSettingsValidation
    {
        /// <summary>
        /// Возвращает список проблем с моделями; пустой список — всё в порядке.
        /// </summary>
        public static IReadOnlyList<string> Validate(this OcrSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var errors = new List<string>();
            var modelsDirectory = settings.ResolveModelsDirectory();

            if (!Directory.Exists(modelsDirectory))
            {
                errors.Add($"Каталог моделей не найден: {modelsDirectory}");
                return errors;
            }

            foreach (var model in GetModelFiles(settings))
            {
                var path = ResolveFilePath(modelsDirectory, model.FileName);

                if (!File.Exists(path))
                {
                    errors.Add($"Не найдена модель «{model.Role}»: {path}");
                    continue;
                }

                if (new FileInfo(path).Length == 0)
                    errors.Add($"Файл модели «{model.Role}» пустой: {path}");
            }

            return errors;
        }

        /// <summary>
        /// Роли и имена файлов всех моделей, используемых пайплайном.
        /// </summary>
        public static IEnumerable<(string Role, string FileName)> GetModelFiles(OcrSettings settings)
        {
            yield return ("детектор текста", settings.DetModel);
            yield return ("классификатор ориентации строк", settings.ClsModel);
            yield return ("распознаватель текста", settings.RecModel);
            yield return ("словарь символов", settings.DictFile);
            yield return ("layout-детектор", settings.DrawingLayoutDetModel);
            yield return ("классификатор ориентации страницы", settings.DocOrientationClsModel);
            yield return ("детектор ячеек таблиц", settings.WiredTableCellDetModel);
        }

        private static string ResolveFilePath(string modelsDirectory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return modelsDirectory;

            return Path.IsPathRooted(fileName)
                ? fileName
                : Path.Combine(modelsDirectory, fileName);
        }
    }

    /// <summary>
    /// Валидатор <see cref="OcrSettings"/> для Options-инфраструктуры:
    /// при старте (ValidateOnStart в WebApi) и при изменении конфигурации
    /// приложение получит понятный список отсутствующих моделей.
    /// </summary>
    public sealed class OcrSettingsValidator : IValidateOptions<OcrSettings>
    {
        public ValidateOptionsResult Validate(string name, OcrSettings options)
        {
            var errors = options.Validate();

            return errors.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(errors);
        }
    }
}
