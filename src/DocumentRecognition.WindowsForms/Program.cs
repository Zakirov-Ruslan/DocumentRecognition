using DocumentRecognition.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentRecognition.WindowsForms
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OcrSettings:ModelsDir"] = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OnnxModels"),
                    ["OcrSettings:DetModel"] = "ch_PP-OCRv5_det_server.onnx",
                    ["OcrSettings:ClsModel"] = "ch_PP-LCNet_x1_0_textline_ori_cls_server.onnx",
                    ["OcrSettings:RecModel"] = "rec_gost_cyrillic_v5.onnx",
                    ["OcrSettings:DictFile"] = "ppocrv5_cyrillic_dict.txt",
                    ["OcrSettings:DrawingLayoutDetModel"] = "drawing_det_v3.onnx",
                    ["OcrSettings:DocOrientationClsModel"] = "doc_orientation_cls.onnx"
                })
                .Build();

            var services = new ServiceCollection();

            services.AddApplicationServices(configuration);
            services.AddTransient<Form1>();

            var serviceProvider = services.BuildServiceProvider();

            var mainForm = serviceProvider.GetRequiredService<Form1>();

            System.Windows.Forms.Application.Run(mainForm);
        }
    }
}