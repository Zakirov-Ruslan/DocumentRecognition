using System;
using System.IO;
using System.Threading.Tasks;

namespace DocumentRecognition.Core.Interfaces
{
    public interface IPdfDocument : IDisposable
    {
        Stream ConvertPageToJpegAsync(int pageIndex, int dpi = 300);
        string ExtractText();
    }

    public interface IPdfServiceFactory
    {
        Task<IPdfDocument> OpenDocumentAsync(Stream inputStream);
    }
}
