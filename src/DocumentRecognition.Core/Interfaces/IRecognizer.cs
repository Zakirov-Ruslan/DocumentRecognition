using DocumentRecognition.Core.Models.Results;
using System.IO;
using System.Threading.Tasks;

namespace DocumentRecognition.Core.Interfaces
{
    /// <summary>
    /// Контракт распознавателя документа.
    /// Сейчас поддерживается только распознавание чертежей (<see cref="RecognizeDrawing"/>).
    /// </summary>
    /// <remarks>
    /// Задел на будущее: для других видов документов сюда добавляются отдельные методы
    /// (например, RecognizeSpecification), чтобы не ломать существующие реализации.
    /// </remarks>
    public interface IRecognizer
    {
        Task<DrawingRecognitionResult> RecognizeDrawing(Stream jpegStream, int conversionDpi);
    }
}
