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
            services.Configure<OcrSettings>(a => configuration.GetSection(OcrSettings.SectionName));

            services.AddSingleton<IRecognizer, RecognitionService>();
            services.AddSingleton<IPdfDocument, MuPdfDocumentWrapper>();

            services.AddScoped<DocumentRecognitionService>();

            services.AddScoped<IPdfServiceFactory, MuPdfServiceFactory>();

            services.AddScoped<DrawingLayoutDetector>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<OcrSettings>>().Value;
                var modelsPath = Path.Combine(AppContext.BaseDirectory, settings.ModelsDir);
                var modelPath = Path.Combine(modelsPath, settings.DrawingLayoutDetModel);

                return new DrawingLayoutDetector(modelPath);
            });

            services.AddScoped<DocOrientationClassifier>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<OcrSettings>>().Value;
                var modelsPath = Path.Combine(AppContext.BaseDirectory, settings.ModelsDir);
                var modelPath = Path.Combine(modelsPath, settings.DocOrientationClsModel);

                return new DocOrientationClassifier(modelPath);
            });

            services.AddScoped<WiredTableCellDetector>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<OcrSettings>>().Value;
                var modelsPath = Path.Combine(AppContext.BaseDirectory, settings.ModelsDir);
                var modelPath = Path.Combine(modelsPath, settings.WiredTableCellDetModel);

                return new WiredTableCellDetector(modelPath);
            });

            services.AddScoped<OcrLite>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<OcrSettings>>().Value;
                var path = Path.Combine(AppContext.BaseDirectory, settings.ModelsDir);

                var engine = new OcrLite
                {
                    DetPath = Path.Combine(path, settings.DetModel),
                    ClsPath = Path.Combine(path, settings.ClsModel),
                    RecPath = Path.Combine(path, settings.RecModel),
                    KeyDicPath = Path.Combine(path, settings.DictFile),
                };

                // Dont call InitModels here, it will be invoked async
                return engine;
            });

            return services;
        }
    }

    public class OcrSettings
    {
        public const string SectionName = "OcrSettings";

        public string ModelsDir { get; set; } = "OnnxModels";
        public string DetModel { get; set; } = "ch_PP-OCRv5_det_server.onnx";
        public string ClsModel { get; set; } = "ch_PP-LCNet_x1_0_textline_ori_cls_server.onnx";
        public string RecModel { get; set; } = "rec_gost_cyrillic_v62.onnx";
        public string DictFile { get; set; } = "ppocrv5_cyrillic_dict.txt";
        public string DrawingLayoutDetModel { get; set; } = "drawing_det_v4.onnx";
        public string DocOrientationClsModel { get; set; } = "techdoc_ori_cls.onnx";
        public string WiredTableCellDetModel { get; set; } = "RT-DETR-L_wired_table_cell_det.onnx";

    }
}
