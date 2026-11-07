using DocumentRecognition.Core.Interfaces;
using DocumentRecognition.Core.Models.Results;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DocumentRecognition.Application.Services
{
    /// <summary>
    /// Оркестратор распознавания документов.
    /// Сейчас поддерживается только распознавание чертежей: PDF постранично
    /// конвертируется в изображение и передаётся в <see cref="IRecognizer"/>.
    /// </summary>
    /// <remarks>
    /// Задел на будущее: при появлении других видов документов (спецификации, ведомости и т.п.)
    /// здесь появится выбор распознавателя, а результаты будут возвращаться через базовый
    /// <see cref="RecognitionResult"/>. Существующий контракт менять не придётся —
    /// достаточно добавить отдельный метод (например, ProcessSpecificationAsync).
    /// </remarks>
    public class DocumentRecognitionService
    {
        private readonly IPdfServiceFactory _pdfFactory;
        private readonly IRecognizer _drawingRecognizer;

        public DocumentRecognitionService(IPdfServiceFactory pdfFactory, IRecognizer drawingRecognizer)
        {
            _pdfFactory = pdfFactory ?? throw new ArgumentNullException(nameof(pdfFactory));
            _drawingRecognizer = drawingRecognizer ?? throw new ArgumentNullException(nameof(drawingRecognizer));
        }

        /// <summary>
        /// Распознаёт чертёж (первую страницу PDF) и возвращает основную надпись и технические требования.
        /// </summary>
        public async Task<DrawingRecognitionResult> ProcessAsync(Stream inputStream, int dpi = 200)
        {
            if (inputStream == null)
                throw new ArgumentNullException(nameof(inputStream));

            using (var pdfDoc = await _pdfFactory.OpenDocumentAsync(inputStream))
            {
                // TODO: Использовать встроенный текстовый слой PDF (pdfDoc.ExtractText()),
                // когда понадобится гибридный режим распознавания.
                using (var jpegStream = pdfDoc.ConvertPageToJpegAsync(0, dpi))
                {
                    return await _drawingRecognizer.RecognizeDrawing(jpegStream, dpi);
                }
            }
        }
    }
}