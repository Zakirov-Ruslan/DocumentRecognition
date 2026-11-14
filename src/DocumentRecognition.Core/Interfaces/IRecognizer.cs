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
        /// <param name="conversionDpi">DPI, с которым был растеризован документ.</param>
        /// <param name="cleanImage">
        /// <c>true</c> — вернуть изображение без отрисованных отладочных рамок детекций
        /// (координаты блоков доступны через <see cref="DrawingRecognitionResult.Regions"/>).
        /// </param>
        Task<DrawingRecognitionResult> RecognizeDrawing(Stream jpegStream, int conversionDpi, bool cleanImage = true);
    }
}
