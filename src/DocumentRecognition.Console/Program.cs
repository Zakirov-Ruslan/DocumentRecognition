using System.CommandLine;
using System.IO;

namespace DocumentRecognition.Console
{
    internal class Program
    {
        private const string Description = "Распознавание чертежей";

        static int Main(string[] args)
        {
            var fileNameOption = new Option<FileInfo>(name: "--filename");
            fileNameOption.Description = "Путь к PDF-файлу с чертежом";
            fileNameOption.Required = true;
            fileNameOption.AcceptLegalFilePathsOnly();

            var rootCommand = new RootCommand(Description);
            rootCommand.Add(fileNameOption);

            rootCommand.SetAction(parseRes =>
            {
                FileInfo parsedFile = parseRes.GetValue(fileNameOption);

                // TODO: Подключить DocumentRecognitionService и вывести результат (текст/JSON).
                using (var fs = parsedFile.OpenRead())
                {
                }

                return 0;
            });

            ParseResult parseResult = rootCommand.Parse(args);
            return parseResult.Invoke();
        }
    }
}
