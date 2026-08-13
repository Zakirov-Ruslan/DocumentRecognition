using DocumentRecognition.OnnxRecognizer;
using Ocr.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DocumentRecognition.Infrastructure.Services
{
    public class StampCellTemplate
    {
        public string Id { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double Wmm { get; set; }
        public double Hmm { get; set; }
        public Rect BoundingBoxPx { get; set; }
    }

    public class MatchPair
    {
        public StampCellTemplate TemplateCell { get; set; }
        public TableCellDetection Detection { get; set; }
        public int DetectionIndex { get; set; }
    }

    public class TemplateEvaluation
    {
        public List<MatchPair> MatchedDetections { get; set; } = new List<MatchPair>();
        public Dictionary<int, int> UsageCount { get; set; } = new Dictionary<int, int>();
    }

    public class CellMatchResult
    {
        public string CellId { get; set; }
        public Rect ExtractedRoi { get; set; }
        public string Text { get; set; }
        public int OcrBlocksCount { get; set; } 

        public bool UsedFallback { get; set; }
        public TableCellDetection MatchedDetection { get; set; }
    }

    public class TitleBlockProcessor
    {
        // Генерация эталонной сетки по ГОСТ 2.104-68 в миллиметрах
        private List<StampCellTemplate> GenerateTemplate(bool hasExtraRow)
        {
            var cells = new List<StampCellTemplate>();
            double yOffset = hasExtraRow ? 8.0 : 0.0;

            if (hasExtraRow)
            {
                cells.Add(new StampCellTemplate { Id = "extra_row", Xmm = 0, Ymm = 0, Wmm = 185, Hmm = 8 });
            }

            // Левая часть: 11 строк по 5 мм. Столбцы: 7, 10, 23, 15, 10 (сумма 65 мм)
            double[] colWidthsTopLeft = { 7, 10, 23, 15, 10 };
            double[] colWidthsBottomLeft = { 17, 23, 15, 10 };
            for (int row = 0; row < 4; row++)
            {
                double xCurrent = 0;
                for (int col = 0; col < colWidthsTopLeft.Length; col++)
                {
                    cells.Add(new StampCellTemplate
                    {
                        Id = $"left_r{row}_c{col}",
                        Xmm = xCurrent,
                        Ymm = yOffset + row * 5,
                        Wmm = colWidthsTopLeft[col],
                        Hmm = 5
                    });
                    xCurrent += colWidthsTopLeft[col];
                }
            }

            for (int row = 4; row < 11; row++)
            {
                double xCurrent = 0;
                for (int col = 0; col < colWidthsBottomLeft.Length; col++)
                {
                    cells.Add(new StampCellTemplate
                    {
                        Id = $"left_r{row}_c{col}",
                        Xmm = xCurrent,
                        Ymm = yOffset + row * 5,
                        Wmm = colWidthsBottomLeft[col],
                        Hmm = 5
                    });
                    xCurrent += colWidthsBottomLeft[col];
                }
            }

            // Правая часть (ширина 120 мм)
            double rightX = 65;

            // 1. Обозначение
            cells.Add(new StampCellTemplate { Id = "designation", Xmm = rightX, Ymm = yOffset, Wmm = 120, Hmm = 15 });

            // 2. Наименование
            cells.Add(new StampCellTemplate { Id = "name", Xmm = rightX, Ymm = yOffset + 15, Wmm = 70, Hmm = 25 });

            // 3. Литера, Масса, Масштаб (справа от обозначения, ширина 50 мм)
            cells.Add(new StampCellTemplate { Id = "litera_1", Xmm = rightX + 70, Ymm = yOffset + 20, Wmm = 5, Hmm = 15 });
            cells.Add(new StampCellTemplate { Id = "litera_2", Xmm = rightX + 70 + 5, Ymm = yOffset + 20, Wmm = 5, Hmm = 15 });
            cells.Add(new StampCellTemplate { Id = "litera_3", Xmm = rightX + 70 + 10, Ymm = yOffset + 20, Wmm = 5, Hmm = 15 });

            // +15mm right from name cell
            cells.Add(new StampCellTemplate { Id = "mass", Xmm = rightX + 70 + 15, Ymm = yOffset + 20, Wmm = 17, Hmm = 15 });
            cells.Add(new StampCellTemplate { Id = "scale", Xmm = rightX + 70 + 15 + 17, Ymm = yOffset + 20, Wmm = 18, Hmm = 15 });

            // 4. Материал
            cells.Add(new StampCellTemplate { Id = "material", Xmm = rightX, Ymm = yOffset + 40, Wmm = 70, Hmm = 15 });

            // 5. Ячейки справа от материала (ширина 50 мм)
            cells.Add(new StampCellTemplate { Id = "bottom_right", Xmm = rightX + 70, Ymm = yOffset + 40, Wmm = 50, Hmm = 15 });

            return cells;
        }

        private void ApplyScale(List<StampCellTemplate> template, int roiWidth, int roiHeight, double totalHeightMm)
        {
            double pxPerMmX = (double)roiWidth / 185.0;
            double pxPerMmY = (double)roiHeight / totalHeightMm;

            foreach (var cell in template)
            {
                cell.BoundingBoxPx = new Rect(
                    (int)(cell.Xmm * pxPerMmX),
                    (int)(cell.Ymm * pxPerMmY),
                    Math.Max(1, (int)(cell.Wmm * pxPerMmX)),
                    Math.Max(1, (int)(cell.Hmm * pxPerMmY))
                );
            }
        }

        public Dictionary<string, CellMatchResult> ProcessTitleBlockWithOcr(Mat roiMat, OcrResult ocrResult)
        {
            // 1. Определяем наличие дополнительной строки по пропорциям ROI
            bool hasExtraRow = DetectExtraRowByAspect(roiMat);
            double totalHeightMm = hasExtraRow ? 63.0 : 55.0;

            // 2. Генерируем и масштабируем шаблон
            var template = GenerateTemplate(hasExtraRow);
            ApplyScale(template, roiMat.Width, roiMat.Height, totalHeightMm);

            var results = new Dictionary<string, CellMatchResult>();

            // Инициализируем результаты для всех ячеек шаблона
            foreach (var cell in template)
            {
                results[cell.Id] = new CellMatchResult
                {
                    CellId = cell.Id,
                    ExtractedRoi = cell.BoundingBoxPx,
                    Text = string.Empty,
                    OcrBlocksCount = 0
                };
            }

            // 3. Сортируем OCR блоки по чтению (сверху-вниз, слева-направо)
            // Это важно, если в одну ячейку попало несколько строк текста
            var sortedBlocks = ocrResult.TextBlocks
                .Where(tb => !string.IsNullOrWhiteSpace(tb.Text))
                .Select(tb => new { Block = tb, Rect = Cv2.BoundingRect(tb.BoxPoints.ToArray()) })
                .OrderBy(x => x.Rect.Y)
                .ThenBy(x => x.Rect.X)
                .Select(x => x.Block)
                .ToList();

            // 4. Сопоставляем OCR TextBlocks с ячейками шаблона
            foreach (var textBlock in sortedBlocks)
            {
                var ocrRect = Cv2.BoundingRect(textBlock.BoxPoints.ToArray());
                var ocrCenter = new Point(ocrRect.X + ocrRect.Width / 2, ocrRect.Y + ocrRect.Height / 2);

                string bestCellId = null;
                double maxIntersectionArea = 0;

                foreach (var cell in template)
                {
                    // Проверяем пересечение
                    var intersection = ocrRect & cell.BoundingBoxPx;
                    if (intersection.Width > 0 && intersection.Height > 0)
                    {
                        // Приоритет 1: Центр OCR бокса строго внутри ячейки
                        if (cell.BoundingBoxPx.Contains(ocrCenter))
                        {
                            bestCellId = cell.Id;
                            break;
                        }

                        // Приоритет 2: Если центр на границе, выбираем ячейку с максимальной площадью пересечения
                        double interArea = intersection.Width * intersection.Height;
                        if (interArea > maxIntersectionArea)
                        {
                            maxIntersectionArea = interArea;
                            bestCellId = cell.Id;
                        }
                    }
                }

                // Fallback: Если текст вылез за все границы (шум или сильный сдвиг), ищем ближайшую ячейку по центру
                if (bestCellId == null)
                {
                    double minDist = double.MaxValue;
                    foreach (var cell in template)
                    {
                        var cellCenter = new Point(cell.BoundingBoxPx.X + cell.BoundingBoxPx.Width / 2,
                                                   cell.BoundingBoxPx.Y + cell.BoundingBoxPx.Height / 2);
                        double dist = Math.Sqrt(Math.Pow(ocrCenter.X - cellCenter.X, 2) + Math.Pow(ocrCenter.Y - cellCenter.Y, 2));
                        if (dist < minDist)
                        {
                            minDist = dist;
                            bestCellId = cell.Id;
                        }
                    }
                }

                if (bestCellId != null && results.ContainsKey(bestCellId))
                {
                    var cellResult = results[bestCellId];
                    // Добавляем пробел, если в ячейке уже есть текст (например, разбитые строки)
                    if (!string.IsNullOrEmpty(cellResult.Text))
                        cellResult.Text += " ";

                    cellResult.Text += textBlock.Text;
                    cellResult.OcrBlocksCount++;
                }
            }

            return results;
        }

        private bool DetectExtraRowByAspect(Mat roiMat)
        {
            double aspectRatio = (double)roiMat.Width / roiMat.Height;
            // 185 / 55 = 3.36 (без доп), 185 / 63 = 2.93 (с доп)
            // Порог 3.15 — середина между значениями
            return aspectRatio < 3.15;
        }

        public Mat DrawDebugOcr(Mat roiMat, Dictionary<string, CellMatchResult> results, OcrResult ocrResult)
        {
            int thickness = Math.Max(1, (int)(roiMat.Width / 500.0));

            // 1. Рисуем сетку шаблона (Синим)
            foreach (var cell in results.Values)
            {
                Cv2.Rectangle(roiMat, cell.ExtractedRoi, new Scalar(255, 0, 0), 1);

                // Подписываем ID ячейки
                Cv2.PutText(roiMat, $"{cell.CellId}", new Point(cell.ExtractedRoi.X + 2, cell.ExtractedRoi.Y + 12),
                    HersheyFonts.HersheySimplex, 0.3, new Scalar(255, 0, 0), 1);
            }

            // 2. Рисуем боксы OCR (Зеленым)
            if (ocrResult?.TextBlocks != null)
            {
                foreach (var tb in ocrResult.TextBlocks)
                {
                    var rect = Cv2.BoundingRect(tb.BoxPoints.ToArray());
                    Cv2.Rectangle(roiMat, rect, new Scalar(0, 255, 0), 2);
                }
            }

            return roiMat;
        }
    }
}
