using DocumentRecognition.Core.Models.Options;
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
        /// <summary>
        /// Распознаёт чертёж на изображении.
        /// </summary>
        /// <param name="jpegStream">Изображение страницы документа.</param>
        /// <param name="options">
        /// Параметры обработки: DPI растеризации и режим отладочного изображения.
        /// </param>
        Task<DrawingRecognitionResult> RecognizeDrawing(Stream jpegStream, DrawingRecognitionOptions options);
    }
}
