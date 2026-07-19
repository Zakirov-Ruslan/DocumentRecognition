using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace DocumentRecognition.Console
{
    internal class Program
    {
        private const string Description = "Распознавание технической документации";

        static int Main(string[] args)
        {
            var fileNameOption = new Option<FileInfo>(name: "--filename");
            fileNameOption.Description = "Путь к файлу";
            fileNameOption.Required = true;
            fileNameOption.AcceptLegalFilePathsOnly();

            var documentTypeOption = new Option<string>(name: "--doctype");
            fileNameOption.Description = "Тип документа";
            fileNameOption.Required = true;

            var rootCommand = new RootCommand(Description);
            rootCommand.Add(fileNameOption);

            rootCommand.SetAction(parseRes =>
            {
                FileInfo parsedFile = parseRes.GetValue(fileNameOption);
                //if (!File.Exists(parsedFile.FullName))
                //    return 

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
