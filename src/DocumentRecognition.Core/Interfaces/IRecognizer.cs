using DocumentRecognition.Core.Models.Results;
using System.IO;
using System.Threading.Tasks;

namespace DocumentRecognition.Core.Interfaces
{
    public interface IRecognizer
    {
        Task<DrawingRecognitionResult> RecognizeDrawing(Stream jpegStream, int conversionDpi);
    }
}
