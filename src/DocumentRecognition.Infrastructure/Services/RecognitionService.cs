using DocumentRecognition.Core.Interfaces;
using DocumentRecognition.Core.Models.Results;
using DocumentRecognition.OnnxRecognizer;
using Ocr;
using OpenCvSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentRecognition.Infrastructure.Services
{
    public class RecognitionService : IRecognizer
    {
        private readonly DocOrientationClassifier _docOrientationClassifier;
        private readonly DrawingLayoutDetector _drawingLayoutDetector;
        private readonly OcrLite _ocrLite;
        private readonly Lazy<Task> _initTask;

        public RecognitionService(OcrLite ocrLite, DrawingLayoutDetector drawingLayoutDetector, DocOrientationClassifier docOrientationClassifier)
        {
            _ocrLite = ocrLite;
            _initTask = new Lazy<Task>(() => _ocrLite.InitModels());
            _drawingLayoutDetector = drawingLayoutDetector;
            _docOrientationClassifier = docOrientationClassifier;
        }

        public async Task<DrawingRecognitionResult> RecognizeDrawing(Stream jpegStream, int conversionDpi)
        {
            await _initTask.Value;

            if (jpegStream.CanSeek)
            {
                jpegStream.Seek(0, SeekOrigin.Begin);
            }

            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                await jpegStream.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            // mm to px scale
            double scale = conversionDpi / 25.4;

            using (Mat srcMat = Cv2.ImDecode(imageBytes, ImreadModes.Color))
            {
                using (var orientedMat = _docOrientationClassifier.EnsureImageOrientation(srcMat))
                {
                    var layoutDetections = _drawingLayoutDetector.Detect(orientedMat);

                    var technicalRequirements = layoutDetections.Where(d => d.Label == "technical_requirements")
                        .OrderByDescending(d => d.Box.X)
                        .Select(d =>
                        {
                            using (Mat roiMat = new Mat(orientedMat, d.Box))
                            {
                                var ocrResult = _ocrLite.Detect(
                                    roiMat,
                                    padding: (int)(20 * scale),
                                    doAngle: true,
                                    mostAngle: false,
                                    unClipRatio: 1.2f,
                                    boxThresh: 0.2f,
                                    boxScoreThresh: 0.25f,
                                    maxSideLen: 1024,
                                    onlyHorizontalLines: true
                                );

                                // Cv2.ImShow("Detected boxes", ocrResult.BoxImg);

                                return ocrResult.StrRes;
                            }
                        }).ToArray();

                    byte[] debugImage = _drawingLayoutDetector.DrawDetections(orientedMat, layoutDetections);

                    DrawingRecognitionResult res = new DrawingRecognitionResult(null, technicalRequirements, debugImage);

                    return res;
                }
            }
        }
    }
}
