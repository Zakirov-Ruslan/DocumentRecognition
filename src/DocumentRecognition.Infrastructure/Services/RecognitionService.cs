using DocumentRecognition.Core.Interfaces;
using DocumentRecognition.Core.Models.Results;
using DocumentRecognition.OnnxRecognizer;
using Ocr;
using Ocr.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentRecognition.Infrastructure.Services
{
    public class RecognitionService : IRecognizer
    {
        private readonly DocOrientationClassifier _docOrientationClassifier;
        private readonly DrawingLayoutDetector _drawingLayoutDetector;
        private readonly WiredTableCellDetector _wiredTableCellDetector;
        private readonly OcrLite _ocrLite;
        private readonly Lazy<Task> _initTask;

        public RecognitionService(OcrLite ocrLite, DrawingLayoutDetector drawingLayoutDetector, DocOrientationClassifier docOrientationClassifier, WiredTableCellDetector wiredTableCellDetector)
        {
            _ocrLite = ocrLite;
            _initTask = new Lazy<Task>(() => _ocrLite.InitModels());
            _drawingLayoutDetector = drawingLayoutDetector;
            _docOrientationClassifier = docOrientationClassifier;
            _wiredTableCellDetector = wiredTableCellDetector;
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

                    var detectionsDict = layoutDetections
                        .GroupBy(d => d.Label)
                        .ToDictionary(gr => gr.Key);

                    string[] techReqs = null;
                    if (detectionsDict.TryGetValue("technical_requirements", out var technicalRequirements))
                    {
                        techReqs = technicalRequirements.OrderByDescending(d => d.Box.X)
                        .Select(d =>
                        {
                            using (Mat roiMat = new Mat(orientedMat, d.Box))
                            {
                                var options = new DetectOptions(
                                    padding: (int)(20 * scale),
                                    doAngle: true,
                                    mostAngle: false,
                                    unClipRatio: 1.5f,
                                    boxThresh: 0.2f,
                                    boxScoreThresh: 0.25f,
                                    maxSideLen: 1024,
                                    onlyHorizontalLines: true,
                                    useBatching: true,
                                    batchSize: 32
                                );

                                var ocrResult = _ocrLite.Detect(roiMat, options);

                                // Cv2.ImShow("tech reqs", ocrResult.BoxImg);

                                return ocrResult.StrRes;
                            }
                        }).ToArray();
                    }

                    Dictionary<string, CellMatchResult> matchedCells = null;
                    if (detectionsDict.TryGetValue("title_block_2", out var titleBlocks))
                    {
                        var titleBlock = titleBlocks.OrderByDescending(tb => tb.Box.X + tb.Box.Y).First();

                        using (Mat roiMat = new Mat(orientedMat, titleBlock.Box))
                        {
                            var ocrOptions = new DetectOptions(
                                padding: 50,
                                doAngle: true,
                                mostAngle: false,
                                unClipRatio: 1.5f, 
                                boxThresh: 0.3f,
                                boxScoreThresh: 0.3f,
                                maxSideLen: 1024,
                                onlyHorizontalLines: false,
                                useBatching: true,
                                batchSize: 32
                            );

                            var titleBlockOcrResult = _ocrLite.Detect(roiMat, ocrOptions);

                            var processor = new TitleBlockProcessor();
                            matchedCells = processor.ProcessTitleBlockWithOcr(roiMat, titleBlockOcrResult);

                            processor.DrawDebugOcr(roiMat, matchedCells, titleBlockOcrResult);

                            // Cv2.ImShow("Detected boxes", roiMat);
                        }
                    }

                    if (detectionsDict.TryGetValue("additional_graphs_left", out var additionalGraphsLeftGroup))
                    {
                        var additionalGraphs = additionalGraphsLeftGroup.OrderBy(gr => gr.Box.Y).First();

                        using (Mat roiMat = new Mat(orientedMat, additionalGraphs.Box))
                        {
                            var additionalGraphsLeftCells = _wiredTableCellDetector.Detect(roiMat);

                            _wiredTableCellDetector.DrawDetections(roiMat, additionalGraphsLeftCells);
                        }
                    }

                    if (detectionsDict.TryGetValue("configuration_table", out var configurationTableGroup))
                    {
                        var configurationTable = configurationTableGroup.First();

                        using (Mat roiMat = new Mat(orientedMat, configurationTable.Box))
                        {
                            var configTableCells = _wiredTableCellDetector.Detect(roiMat);

                            _wiredTableCellDetector.DrawDetections(roiMat, configTableCells);
                        }
                    }

                    if (detectionsDict.TryGetValue("parameter_table", out var parameterTableGroup))
                    {
                        var parameterTable = parameterTableGroup.First();

                        using (Mat roiMat = new Mat(orientedMat, parameterTable.Box))
                        {
                            var parameterTableCells = _wiredTableCellDetector.Detect(roiMat);

                            _wiredTableCellDetector.DrawDetections(roiMat, parameterTableCells);
                        }
                    }

                    var debugImage = _drawingLayoutDetector.DrawDetections(orientedMat, layoutDetections);

                    List<Signature> signatures = new List<Signature>();
                    for (int row = 5; row <= 10; row++)
                    {
                        string signatoryText = matchedCells[$"left_r{row}_c{0}"].Text;

                        if (string.IsNullOrEmpty(signatoryText))
                            continue;

                        string name = matchedCells[$"left_r{row}_c{1}"].Text;
                        DateTime.TryParse(matchedCells[$"left_r{row}_c{3}"].Text, out var signDate);

                        Signature signature = new Signature()
                        {
                            SignatoryText = signatoryText,
                            Name = name,
                            Date = signDate
                        };

                        signatures.Add(signature);
                    }

                    TitleBlock titleBlockResult = new TitleBlock()
                    { 
                        Designation = matchedCells.ContainsKey("designation") ? matchedCells["designation"].Text : default,
                        Name = matchedCells.ContainsKey("name") ? matchedCells["name"].Text : default,
                        Mass = matchedCells.ContainsKey("mass") ? matchedCells["mass"].Text : default,
                        Scale = matchedCells.ContainsKey("scale") ? matchedCells["scale"].Text : default,
                        Material = matchedCells.ContainsKey("material") ? matchedCells["material"].Text : default,
                        SheetNumber = matchedCells.ContainsKey("sheet_number") ? matchedCells["sheet_number"].Text : default,
                        TotalSheets = matchedCells.ContainsKey("total_sheets") ? matchedCells["total_sheets"].Text : default,

                        Signatures = signatures
                    };

                    DrawingRecognitionResult res = new DrawingRecognitionResult(titleBlockResult, techReqs, debugImage.ToBytes());

                    return res;
                }
            }
        }
    }
}
