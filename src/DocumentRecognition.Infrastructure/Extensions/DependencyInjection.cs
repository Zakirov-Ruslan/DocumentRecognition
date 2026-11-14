using DocumentRecognition.Application.Services;
using DocumentRecognition.Core.Interfaces;
using DocumentRecognition.Infrastructure.Services;
using DocumentRecognition.OnnxRecognizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocr;
using System;
using System.IO;

namespace DocumentRecognition.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Секция OcrSettings из IConfiguration накладывается поверх значений по умолчанию
            // из самого класса OcrSettings (Bind только перезаписывает заданные ключи).
            services.AddOptions<OcrSettings>()
                .Bind(configuration.GetSection(OcrSettings.SectionName))
                .ValidateOnStart();

            // Проверка наличия моделей: ошибка всплывает при старте/обращении к настройкам,
            // а не посреди распознавания.
            services.AddSingleton<IValidateOptions<OcrSettings>, OcrSettingsValidator>();

            // Нужен для смены настроек движков в рантайме (WinForms/WebApi reloadOnChange).
            services.AddSingleton(configuration);
            services.AddSingleton<IRuntimeOcrSettings, RuntimeOcrSettings>();

            // Тяжёлые зависимости (модели ONNX) создаются лениво внутри RecognitionService
            // и пересоздаются при изменении OcrSettings без перезапуска приложения.
            services.AddSingleton<IRecognizer, RecognitionService>();
            services.AddSingleton<IPdfServiceFactory, MuPdfServiceFactory>();

            services.AddScoped<DocumentRecognitionService>();

            return services;
        }
    }

    public class OcrSettings
    {
        public const string SectionName = "OcrSettings";

        public string ModelsDir { get; set; } = "OnnxModels";
        public string DetModel { get; set; } = "ch_PP-OCRv5_det_server.onnx";
        public string ClsModel { get; set; } = "ch_PP-LCNet_x1_0_textline_ori_cls_server.onnx";
        public string RecModel { get; set; } = "rec_gost_cyrillic_v7.onnx";
        public string DictFile { get; set; } = "ppocrv5_cyrillic_dict.txt";
        public string DrawingLayoutDetModel { get; set; } = "drawing_det_v4.onnx";
        public string DocOrientationClsModel { get; set; } = "techdoc_ori_cls.onnx";
        public string WiredTableCellDetModel { get; set; } = "RT-DETR-L_wired_table_cell_det.onnx";

        /// <summary>
        /// Провайдер выполнения ONNX-инференса.
        /// </summary>
        public InferenceExecutionProvider Provider { get; set; } = InferenceExecutionProvider.Auto;

        /// <summary>
        /// Количество потоков CPU для ONNX-сессий (0 — авто).
        /// </summary>
        public int Threads { get; set; }

        /// <summary>
        /// Каталог моделей: относительный путь считается от каталога приложения,
        /// абсолютный используется как есть.
        /// </summary>
        public string ResolveModelsDirectory()
        {
            if (string.IsNullOrWhiteSpace(ModelsDir))
                return AppContext.BaseDirectory;

            return Path.IsPathRooted(ModelsDir)
                ? ModelsDir
                : Path.Combine(AppContext.BaseDirectory, ModelsDir);
        }

        /// <summary>
        /// Копия настроек (для изменения в UI без мутации текущего снимка).
        /// </summary>
        public OcrSettings Copy()
        {
            return (OcrSettings)MemberwiseClone();
        }
    }
}
