using DocumentRecognition.Core.Enums;
using DocumentRecognition.Core.Interfaces;
using DocumentRecognition.Core.Models;
using DocumentRecognition.Core.Models.Options;
using DocumentRecognition.Core.Models.Results;
using DocumentRecognition.Infrastructure.Extensions;
using DocumentRecognition.OnnxRecognizer;
using Microsoft.Extensions.Options;
using Ocr;
using Ocr.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DocumentRecognition.Infrastructure.Services
{
    public class RecognitionService : IRecognizer
    {
        private readonly IOptionsMonitor<OcrSettings> _settingsMonitor;
        private readonly SemaphoreSlim _recognitionLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Активный набор движков. Пересоздаётся под новый снимок настроек
        /// при следующем распознавании — без перезапуска приложения.
        /// </summary>
        private RecognitionEngines _engines;

        public RecognitionService(IOptionsMonitor<OcrSettings> settingsMonitor)
        {
            _settingsMonitor = settingsMonitor ?? throw new ArgumentNullException(nameof(settingsMonitor));
        }

        /// <summary>
        /// Возвращает набор движков, соответствующий текущим настройкам.
        /// Вызывается под <see cref="_recognitionLock"/>, поэтому подмена движков
        /// не может произойти во время распознавания.
        /// </summary>
        private async Task<RecognitionEngines> GetEnginesAsync()
        {
            var settings = _settingsMonitor.CurrentValue;
            var current = _engines;

            if (current != null && current.Matches(settings))
                return current;

            var created = RecognitionEngines.Create(settings, current);

            try
            {
                // Загружаются только новые движки, переиспользованные — no-op.
                await created.InitializeAsync().ConfigureAwait(false);
            }
            catch
            {
                // Новые модели поднялись неудачно — освобождаем только созданное,
                // старый набор продолжаем использовать.
                created.DisposeExcept(current);
                throw;
            }

            _engines = created;
            current?.DisposeExcept(created);

            Console.WriteLine("[RecognitionService] Набор inference-движков пересоздан под новые настройки.");

            return created;
        }

        public async Task<DrawingRecognitionResult> RecognizeDrawing(Stream jpegStream, DrawingRecognitionOptions options)
        {
            if (options == null)
                options = new DrawingRecognitionOptions();

            options.Normalize();

            // OcrLite и сессии ONNX не рассчитаны на параллельные вызовы,
            // поэтому распознавание сериализуется (актуально для WebApi).
            await _recognitionLock.WaitAsync().ConfigureAwait(false);

            try
            {
                var engines = await GetEnginesAsync().ConfigureAwait(false);

                return await RecognizeDrawingCore(jpegStream, options, engines).ConfigureAwait(false);
            }
            finally
            {
                _recognitionLock.Release();
            }
        }

        private async Task<DrawingRecognitionResult> RecognizeDrawingCore(
            Stream jpegStream,
            DrawingRecognitionOptions options,
            RecognitionEngines engines)
        {
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
            double scale = options.Dpi / 25.4;

            var cleanImage = options.CleanImage;

            using (Mat srcMat = Cv2.ImDecode(imageBytes, ImreadModes.Color))
            {
                using (var orientedMat = engines.OrientationClassifier.EnsureImageOrientation(srcMat))
                {
                    var layoutDetections = engines.LayoutDetector.Detect(orientedMat);

                    var detectionsDict = layoutDetections
                        .GroupBy(d => d.Label)
                        .ToDictionary(gr => gr.Key);

                    // Регионы результата: используются UI для подсветки и тултипов.
                    // Координаты всех регионов приводятся к системе полного изображения.
                    var regions = new List<RecognitionRegion>();
                    AddLayoutBlockRegions(regions, layoutDetections);

                    string[] techReqs = null;
                    if (detectionsDict.TryGetValue("technical_requirements", out var technicalRequirements))
                    {
                        var orderedTechnicalRequirements = technicalRequirements
                            .OrderByDescending(d => d.Box.X)
                            .ToList();

                        var technicalRequirementLines = new List<string>(orderedTechnicalRequirements.Count);

                        for (var requirementIndex = 0; requirementIndex < orderedTechnicalRequirements.Count; requirementIndex++)
                        {
                            var detection = orderedTechnicalRequirements[requirementIndex];

                            using (Mat roiMat = new Mat(orientedMat, detection.Box))
                            {
                                var detectOptions = new DetectOptions(
                                    padding: (int)(20 * scale),
                                    doAngle: false,
                                    mostAngle: false,
                                    unClipRatio: 1.5f,
                                    boxThresh: 0.2f,
                                    boxScoreThresh: 0.25f,
                                    maxSideLen: 1024,
                                    onlyHorizontalLines: true,
                                    useBatching: true,
                                    batchSize: 32
                                );

                                var ocrResult = engines.OcrLite.Detect(roiMat, detectOptions);

                                technicalRequirementLines.Add(ocrResult.StrRes);

                                // Все OCR-строки одного блока требований получают общий ключ:
                                // он связывает наведение на изображение с пунктом списка в UI.
                                AddOcrLineRegions(
                                    regions,
                                    ocrResult,
                                    detection.Box.X,
                                    detection.Box.Y,
                                    label: "technical_requirements",
                                    key: $"technical_requirements:{requirementIndex}");
                            }
                        }

                        techReqs = technicalRequirementLines.ToArray();
                    }

                    Dictionary<string, CellMatchResult> matchedCells = null;
                    if (detectionsDict.TryGetValue("title_block_2", out var titleBlocks))
                    {
                        var titleBlock = titleBlocks.OrderByDescending(tb => tb.Box.X + tb.Box.Y).First();

                        using (Mat roiMat = new Mat(orientedMat, titleBlock.Box))
                        {
                            var ocrOptions = new DetectOptions(
                                padding: 50,
                                doAngle: false,
                                mostAngle: false,
                                unClipRatio: 1.5f, 
                                boxThresh: 0.3f,
                                boxScoreThresh: 0.3f,
                                maxSideLen: 1024,
                                onlyHorizontalLines: false,
                                useBatching: true,
                                batchSize: 32
                            );

                            var titleBlockOcrResult = engines.OcrLite.Detect(roiMat, ocrOptions);

                            var processor = new TitleBlockProcessor();
                            matchedCells = processor.ProcessTitleBlockWithOcr(roiMat, titleBlockOcrResult);

                            AddTitleBlockCellRegions(regions, matchedCells, titleBlock.Box.X, titleBlock.Box.Y);
                            AddOcrLineRegions(
                                regions,
                                titleBlockOcrResult,
                                titleBlock.Box.X,
                                titleBlock.Box.Y,
                                label: "title_block",
                                key: null);

                            if (!cleanImage)
                                processor.DrawDebugOcr(roiMat, matchedCells, titleBlockOcrResult);

                            // Cv2.ImShow("Detected boxes", roiMat);
                        }
                    }

                    if (detectionsDict.TryGetValue("additional_graphs_left", out var additionalGraphsLeftGroup))
                    {
                        var additionalGraphs = additionalGraphsLeftGroup.OrderBy(gr => gr.Box.Y).First();

                        using (Mat roiMat = new Mat(orientedMat, additionalGraphs.Box))
                        {
                            var additionalGraphsLeftCells = engines.TableCellDetector.Detect(roiMat);

                            // Рамки рисуются только для отладочного изображения;
                            // при cleanImage координаты доступны через Regions.
                            if (!cleanImage)
                                engines.TableCellDetector.DrawDetections(roiMat, additionalGraphsLeftCells);

                            AddTableCellRegions(
                                regions,
                                additionalGraphsLeftCells,
                                additionalGraphs.Box.X,
                                additionalGraphs.Box.Y,
                                additionalGraphs.Label);
                        }
                    }

                    if (detectionsDict.TryGetValue("configuration_table", out var configurationTableGroup))
                    {
                        var configurationTable = configurationTableGroup.First();

                        using (Mat roiMat = new Mat(orientedMat, configurationTable.Box))
                        {
                            var configTableCells = engines.TableCellDetector.Detect(roiMat);

                            if (!cleanImage)
                                engines.TableCellDetector.DrawDetections(roiMat, configTableCells);

                            AddTableCellRegions(
                                regions,
                                configTableCells,
                                configurationTable.Box.X,
                                configurationTable.Box.Y,
                                configurationTable.Label);
                        }
                    }

                    if (detectionsDict.TryGetValue("parameter_table", out var parameterTableGroup))
                    {
                        var parameterTable = parameterTableGroup.First();

                        using (Mat roiMat = new Mat(orientedMat, parameterTable.Box))
                        {
                            var parameterTableCells = engines.TableCellDetector.Detect(roiMat);

                            if (!cleanImage)
                                engines.TableCellDetector.DrawDetections(roiMat, parameterTableCells);

                            AddTableCellRegions(
                                regions,
                                parameterTableCells,
                                parameterTable.Box.X,
                                parameterTable.Box.Y,
                                parameterTable.Label);
                        }
                    }

                    var debugImage = cleanImage
                        ? orientedMat
                        : engines.LayoutDetector.DrawDetections(orientedMat, layoutDetections);

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

                    DrawingRecognitionResult res = new DrawingRecognitionResult(
                        titleBlockResult,
                        techReqs,
                        debugImage.ToBytes(),
                        regions,
                        orientedMat.Width,
                        orientedMat.Height,
                        options.Dpi);

                    return res;
                }
            }
        }

        /// <summary>
        /// Добавляет регионы блоков разметки чертежа (координаты уже в полном изображении).
        /// </summary>
        private static void AddLayoutBlockRegions(List<RecognitionRegion> regions, List<DrawingLayoutDetection> detections)
        {
            foreach (var detection in detections)
            {
                regions.Add(new RecognitionRegion(
                    ToPixelBox(detection.Box),
                    RecognitionRegionKind.LayoutBlock,
                    label: detection.Label,
                    score: detection.Score));
            }
        }

        /// <summary>
        /// Добавляет регионы ячеек шаблонной сетки основной надписи.
        /// Координаты ячеек заданы внутри ROI, поэтому смещаются на позицию ROI.
        /// </summary>
        private static void AddTitleBlockCellRegions(
            List<RecognitionRegion> regions,
            Dictionary<string, CellMatchResult> matchedCells,
            int offsetX,
            int offsetY)
        {
            if (matchedCells == null)
                return;

            foreach (var cell in matchedCells.Values)
            {
                regions.Add(new RecognitionRegion(
                    ToPixelBox(cell.ExtractedRoi, offsetX, offsetY),
                    RecognitionRegionKind.TitleBlockCell,
                    label: cell.CellId,
                    key: cell.CellId,
                    text: cell.Text,
                    score: cell.MatchedDetection?.Score ?? 0f));
            }
        }

        /// <summary>
        /// Добавляет регионы распознанных OCR-строк внутри ROI с указанным смещением.
        /// </summary>
        private static void AddOcrLineRegions(
            List<RecognitionRegion> regions,
            OcrResult ocrResult,
            int offsetX,
            int offsetY,
            string label,
            string key)
        {
            if (ocrResult?.TextBlocks == null)
                return;

            foreach (var textBlock in ocrResult.TextBlocks)
            {
                if (string.IsNullOrWhiteSpace(textBlock.Text) || textBlock.BoxPoints == null || textBlock.BoxPoints.Count == 0)
                    continue;

                var rect = Cv2.BoundingRect(textBlock.BoxPoints.ToArray());

                regions.Add(new RecognitionRegion(
                    ToPixelBox(rect, offsetX, offsetY),
                    RecognitionRegionKind.OcrLine,
                    label: label,
                    key: key,
                    text: textBlock.Text,
                    score: textBlock.BoxScore));
            }
        }

        /// <summary>
        /// Добавляет регионы ячеек найденной таблицы (текст в них не распознаётся).
        /// </summary>
        private static void AddTableCellRegions(
            List<RecognitionRegion> regions,
            List<TableCellDetection> cells,
            int offsetX,
            int offsetY,
            string tableLabel)
        {
            if (cells == null)
                return;

            for (var index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];

                regions.Add(new RecognitionRegion(
                    ToPixelBox(cell.Box, offsetX, offsetY),
                    RecognitionRegionKind.TableCell,
                    label: tableLabel,
                    key: $"{tableLabel}:{index}",
                    score: cell.Score));
            }
        }

        private static PixelBox ToPixelBox(Rect rect, int offsetX = 0, int offsetY = 0)
        {
            return new PixelBox(rect.X + offsetX, rect.Y + offsetY, rect.Width, rect.Height);
        }
    }
}
