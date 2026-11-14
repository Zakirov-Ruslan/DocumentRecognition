using DocumentRecognition.OnnxRecognizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;

namespace DocumentRecognition.Infrastructure.Extensions
{
    /// <summary>
    /// Доступ к настройкам движков распознавания с возможностью изменить их в рантайме.
    /// </summary>
    /// <remarks>
    /// Изменения записываются в <see cref="IConfiguration"/> и применяются через
    /// <see cref="IOptionsMonitor{TOptions}"/> при следующем распознавании — без перезапуска
    /// приложения. Пересоздание inference-движков выполняет
    /// <c>RecognitionService</c>, когда видит другой снимок настроек.
    /// </remarks>
    public interface IRuntimeOcrSettings
    {
        /// <summary>Текущие действующие настройки.</summary>
        OcrSettings Current { get; }

        /// <summary>
        /// Применяет новые настройки: значения пишутся в конфигурацию,
        /// после чего пересоздаются только изменившиеся движки.
        /// </summary>
        void Apply(OcrSettings settings);
    }

    /// <inheritdoc />
    public sealed class RuntimeOcrSettings : IRuntimeOcrSettings
    {
        private readonly IConfiguration _configuration;
        private readonly IOptionsMonitor<OcrSettings> _monitor;

        public RuntimeOcrSettings(IConfiguration configuration, IOptionsMonitor<OcrSettings> monitor)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        }

        public OcrSettings Current => _monitor.CurrentValue;

        public void Apply(OcrSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            Set(nameof(OcrSettings.ModelsDir), settings.ModelsDir);
            Set(nameof(OcrSettings.DetModel), settings.DetModel);
            Set(nameof(OcrSettings.ClsModel), settings.ClsModel);
            Set(nameof(OcrSettings.RecModel), settings.RecModel);
            Set(nameof(OcrSettings.DictFile), settings.DictFile);
            Set(nameof(OcrSettings.DrawingLayoutDetModel), settings.DrawingLayoutDetModel);
            Set(nameof(OcrSettings.DocOrientationClsModel), settings.DocOrientationClsModel);
            Set(nameof(OcrSettings.WiredTableCellDetModel), settings.WiredTableCellDetModel);
            Set(nameof(OcrSettings.Provider), settings.Provider.ToString());
            Set(nameof(OcrSettings.Threads), settings.Threads.ToString(CultureInfo.InvariantCulture));

            // Сообщаем Options-инфраструктуре, что значения изменились:
            // IOptionsMonitor перечитает секцию и вернёт новый снимок.
            if (_configuration is IConfigurationRoot root)
                root.Reload();
        }

        private void Set(string key, string value)
        {
            _configuration[$"{OcrSettings.SectionName}:{key}"] = value;
        }
    }
}
