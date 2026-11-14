using DocumentRecognition.Core.Interfaces;
using DocumentRecognition.Core.Models.Options;
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
        /// <param name="inputStream">Поток с PDF-документом.</param>
        /// <param name="options">Параметры обработки запроса (DPI, режим отладочного изображения).</param>
        public async Task<DrawingRecognitionResult> ProcessAsync(Stream inputStream, DrawingRecognitionOptions options)
        {
            if (inputStream == null)
                throw new ArgumentNullException(nameof(inputStream));

            if (options == null)
                options = new DrawingRecognitionOptions();

            options.Normalize();

            using (var pdfDoc = await _pdfFactory.OpenDocumentAsync(inputStream))
            {
                // TODO: Использовать встроенный текстовый слой PDF (pdfDoc.ExtractText()),
                // когда понадобится гибридный режим распознавания.
                using (var jpegStream = pdfDoc.ConvertPageToJpegAsync(0, options.Dpi))
                {
                    return await _drawingRecognizer.RecognizeDrawing(jpegStream, options);
                }
            }
        }

        /// <summary>
        /// Перегрузка для простых сценариев: распознавание с указанным DPI.
        /// </summary>
        public Task<DrawingRecognitionResult> ProcessAsync(Stream inputStream, int dpi = DrawingRecognitionOptions.DefaultDpi, bool cleanImage = true)
        {
            return ProcessAsync(inputStream, new DrawingRecognitionOptions
            {
                Dpi = dpi,
                CleanImage = cleanImage
            });
        }
    }
}