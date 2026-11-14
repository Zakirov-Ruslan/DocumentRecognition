using DocumentRecognition.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentRecognition.WindowsForms
{
    internal static class Program
    {
        /// <summary>
        /// Пользовательские настройки движков (провайдер инференса, каталог моделей).
        /// Файл перечитывается на лету (reloadOnChange), поэтому применённые
        /// изменения подхватываются без перезапуска приложения.
        /// </summary>
        internal static string SettingsFilePath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DocumentRecognition",
            "settings.json");

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
                    // Каталог моделей по умолчанию — рядом с приложением.
                    [$"{OcrSettings.SectionName}:{nameof(OcrSettings.ModelsDir)}"] =
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OnnxModels")
                })
                .AddJsonFile(SettingsFilePath, optional: true, reloadOnChange: true)
                // Последний провайдер имеет наивысший приоритет: сюда пишутся
                // изменения из UI, поэтому они действуют до перезапуска.
                .AddInMemoryCollection(new Dictionary<string, string?>())
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