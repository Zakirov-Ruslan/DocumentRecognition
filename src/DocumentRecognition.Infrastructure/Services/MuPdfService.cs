using DocumentRecognition.Core.Interfaces;
using MuPDFCore;
using System;
using System.IO;
using System.Threading.Tasks;


namespace DocumentRecognition.Infrastructure.Services
{
    public class MuPdfDocumentWrapper : IPdfDocument
    {
        private readonly MuPDFContext _context;
        private readonly MuPDFDocument _document;
        private bool _disposed;

        public MuPdfDocumentWrapper(Stream inputStream)
        {
            byte[] pdfBytes;
            using (var ms = new MemoryStream())
            {
                inputStream.CopyTo(ms);
                pdfBytes = ms.ToArray();
            }

            _context = new MuPDFContext();
            _document = new MuPDFDocument(_context, pdfBytes, InputFileTypes.PDF);
        }

        public Stream ConvertPageToJpegAsync(int pageIndex, int dpi = 100)
        {
            ThrowIfDisposed();

            var resultStream = new MemoryStream();

            _document.WriteImageAsJPEG(pageIndex, 1, resultStream, 100, false);

            return resultStream;
        }

        public string ExtractText()
        {
            ThrowIfDisposed();

            return _document.ExtractText(null, false);
        }

        public void Dispose()
        {
            if (_disposed) 
                return;

            _document?.Dispose();
            _context?.Dispose();
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) 
                throw new ObjectDisposedException(nameof(MuPdfDocumentWrapper));
        }
    }

    public class MuPdfServiceFactory : IPdfServiceFactory
    {
        public async Task<IPdfDocument> OpenDocumentAsync(Stream inputStream)
        {
            return new MuPdfDocumentWrapper(inputStream);
        }
    }
}
