using DocumentRecognition.Core.Interfaces;
using DocumentRecognition.Core.Enums;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DocumentRecognition.Application.Services
{
    public class DocumentRecognitionService
    {
        private readonly IPdfServiceFactory _pdfFactory;
        private readonly IRecognizer _textRecognizer;

        public DocumentRecognitionService(IPdfServiceFactory pdfFactory, IRecognizer textRecognizer)
        {
            _pdfFactory = pdfFactory ?? throw new ArgumentNullException(nameof(pdfFactory));
            _textRecognizer = textRecognizer ?? throw new ArgumentNullException(nameof(textRecognizer));
        }

        public async Task<object> ProcessAsync(Stream inputStream, DocumentType? documentType = DocumentType.Drawing, int dpi = 200)
        {
            if (documentType != DocumentType.Drawing)
                throw new NotImplementedException();

            using (var pdfDoc = await _pdfFactory.OpenDocumentAsync(inputStream))
            {
                string nativeText = pdfDoc.ExtractText();

                using (var jpegStream = pdfDoc.ConvertPageToJpegAsync(0, dpi))
                {
                    var result = await _textRecognizer.RecognizeDrawing(jpegStream, dpi);

                    return result;
                }
            }
        }
    }
}