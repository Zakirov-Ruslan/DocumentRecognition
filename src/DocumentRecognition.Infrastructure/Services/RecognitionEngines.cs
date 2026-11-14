using DocumentRecognition.Infrastructure.Extensions;
using DocumentRecognition.OnnxRecognizer;
using Ocr;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DocumentRecognition.Infrastructure.Services
{
    /// <summary>
    /// Набор inference-движков, собранный под конкретный снимок <see cref="Extensions.OcrSettings"/>.
    /// </summary>
    /// <remarks>
    /// При смене настроек пересоздаются только те движки, чьи модели/провайдер/потоки
    /// изменились; остальные переиспользуются вместе с уже загруженными сессиями.
    /// </remarks>
    internal sealed class RecognitionEngines : IDisposable
    {
        private readonly string _ocrSignature;
        private readonly string _layoutSignature;
        private readonly string _orientationSignature;
        private readonly string _tableSignature;

        private bool _disposed;

        private RecognitionEngines(
            OcrLite ocrLite,
            DrawingLayoutDetector layoutDetector,
            DocOrientationClassifier orientationClassifier,
            WiredTableCellDetector tableCellDetector,
            string ocrSignature,
            string layoutSignature,
            string orientationSignature,
            string tableSignature)
        {
            OcrLite = ocrLite;
            LayoutDetector = layoutDetector;
            OrientationClassifier = orientationClassifier;
            TableCellDetector = tableCellDetector;

            _ocrSignature = ocrSignature;
            _layoutSignature = layoutSignature;
            _orientationSignature = orientationSignature;
            _tableSignature = tableSignature;
        }

        public OcrLite OcrLite { get; }

        public DrawingLayoutDetector LayoutDetector { get; }

        public DocOrientationClassifier OrientationClassifier { get; }

        public WiredTableCellDetector TableCellDetector { get; }

        public bool IsInitialized =>
            OcrLite.IsInitialized
            && LayoutDetector.IsInitialized
            && OrientationClassifier.IsInitialized
            && TableCellDetector.IsInitialized;

        /// <summary>
        /// Создаёт набор движков под настройки, переиспользуя подходящие движки из <paramref name="previous"/>.
        /// Предыдущий набор при этом не освобождается — это делает вызывающая сторона
        /// после успешной инициализации нового.
        /// </summary>
        public static RecognitionEngines Create(OcrSettings settings, RecognitionEngines previous)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            GetSignatures(settings, out var ocrSignature, out var layoutSignature, out var orientationSignature, out var tableSignature);

            var modelsDirectory = settings.ResolveModelsDirectory();

            var ocrLite = previous != null && previous._ocrSignature == ocrSignature
                ? previous.OcrLite
                : CreateOcrLite(settings, modelsDirectory);

            var layoutDetector = previous != null && previous._layoutSignature == layoutSignature
                ? previous.LayoutDetector
                : new DrawingLayoutDetector(
                    Path.Combine(modelsDirectory, settings.DrawingLayoutDetModel),
                    settings.Provider,
                    settings.Threads);

            var orientationClassifier = previous != null && previous._orientationSignature == orientationSignature
                ? previous.OrientationClassifier
                : new DocOrientationClassifier(
                    Path.Combine(modelsDirectory, settings.DocOrientationClsModel),
                    settings.Provider,
                    settings.Threads);

            var tableCellDetector = previous != null && previous._tableSignature == tableSignature
                ? previous.TableCellDetector
                : new WiredTableCellDetector(
                    Path.Combine(modelsDirectory, settings.WiredTableCellDetModel),
                    settings.Provider,
                    settings.Threads);

            return new RecognitionEngines(
                ocrLite,
                layoutDetector,
                orientationClassifier,
                tableCellDetector,
                ocrSignature,
                layoutSignature,
                orientationSignature,
                tableSignature);
        }

        /// <summary>
        /// <c>true</c>, если набор соответствует настройкам и пересоздание не требуется.
        /// </summary>
        public bool Matches(OcrSettings settings)
        {
            GetSignatures(settings, out var ocrSignature, out var layoutSignature, out var orientationSignature, out var tableSignature);

            return _ocrSignature == ocrSignature
                && _layoutSignature == layoutSignature
                && _orientationSignature == orientationSignature
                && _tableSignature == tableSignature;
        }

        /// <summary>
        /// Загружает модели. Повторные вызовы для уже загруженных движков — no-op.
        /// </summary>
        public Task InitializeAsync()
        {
            return Task.WhenAll(
                OcrLite.InitializeAsync(),
                LayoutDetector.InitializeAsync(),
                OrientationClassifier.InitializeAsync(),
                TableCellDetector.InitializeAsync());
        }

        /// <summary>
        /// Освобождает движки, которые не входят в <paramref name="keep"/> (переиспользованные не трогает).
        /// </summary>
        public void DisposeExcept(RecognitionEngines keep)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (!ReferenceEquals(OcrLite, keep?.OcrLite))
                OcrLite.Dispose();

            if (!ReferenceEquals(LayoutDetector, keep?.LayoutDetector))
                LayoutDetector.Dispose();

            if (!ReferenceEquals(OrientationClassifier, keep?.OrientationClassifier))
                OrientationClassifier.Dispose();

            if (!ReferenceEquals(TableCellDetector, keep?.TableCellDetector))
                TableCellDetector.Dispose();
        }

        public void Dispose()
        {
            DisposeExcept(null);
        }

        private static OcrLite CreateOcrLite(OcrSettings settings, string modelsDirectory)
        {
            return new OcrLite
            {
                DetPath = Path.Combine(modelsDirectory, settings.DetModel),
                ClsPath = Path.Combine(modelsDirectory, settings.ClsModel),
                RecPath = Path.Combine(modelsDirectory, settings.RecModel),
                KeyDicPath = Path.Combine(modelsDirectory, settings.DictFile),
                Provider = settings.Provider,
                ThreadNum = settings.Threads > 0
                    ? settings.Threads
                    : (int)(Environment.ProcessorCount * 0.7)
            };
        }

        /// <summary>
        /// Подписи конфигурации каждого движка: меняется хотя бы один параметр — движок пересоздаётся.
        /// </summary>
        private static void GetSignatures(
            OcrSettings settings,
            out string ocrSignature,
            out string layoutSignature,
            out string orientationSignature,
            out string tableSignature)
        {
            var modelsDirectory = settings.ResolveModelsDirectory();

            ocrSignature = Signature(
                modelsDirectory,
                settings.DetModel,
                settings.ClsModel,
                settings.RecModel,
                settings.DictFile,
                settings.Provider,
                settings.Threads);

            layoutSignature = Signature(
                Path.Combine(modelsDirectory, settings.DrawingLayoutDetModel),
                settings.Provider,
                settings.Threads);

            orientationSignature = Signature(
                Path.Combine(modelsDirectory, settings.DocOrientationClsModel),
                settings.Provider,
                settings.Threads);

            tableSignature = Signature(
                Path.Combine(modelsDirectory, settings.WiredTableCellDetModel),
                settings.Provider,
                settings.Threads);
        }

        private static string Signature(params object[] parts)
        {
            return string.Join("|", parts);
        }
    }
}
