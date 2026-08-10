/*
 * Modified by Zakirov-Ruslan
 * Original: https://github.com/RapidAI/RapidOCRCSharp
 */

using Microsoft.ML.OnnxRuntime.Tensors;
using Ocr.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Point = OpenCvSharp.Point;

namespace Ocr
{
    internal class OcrUtils
    {
        public static Tensor<float> SubstractMeanNormalize(Mat src, float[] meanVals, float[] normVals)
        {
            int cols = src.Cols;
            int rows = src.Rows;
            int channels = src.Channels();

            Tensor<float> inputTensor = new DenseTensor<float>(new[] { 1, channels, rows, cols });

            for (int c = 0; c < channels; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        float value;
                        if (channels == 3)
                        {
                            // BGR (HWC)
                            var pixel = src.At<Vec3b>(r, col);
                            value = pixel[c];

                        }
                        else
                        {
                            value = src.At<byte>(r, col);
                        }

                        float data = (value - meanVals[c]) * normVals[c];
                        inputTensor[0, c, r, col] = data;
                    }
                }
            }

            return inputTensor;
        }

        public static Mat MakePadding(Mat src, int padding)
        {
            if (padding <= 0) 
                return src;

            Mat paddingSrc = new Mat();
            Cv2.CopyMakeBorder(src, paddingSrc, padding, padding, padding, padding, BorderTypes.Isolated, Scalar.White);

            return paddingSrc;
        }

        public static int GetThickness(Mat boxImg)
        {
            int minSize = boxImg.Cols > boxImg.Rows 
                ? boxImg.Rows 
                : boxImg.Cols;

            int thickness = minSize / 1000 + 2;
            return thickness;
        }

        public static void DrawTextBox(Mat boxImg, List<Point> box, int thickness)
        {
            if (box == null || box.Count == 0)
                return;

            var color = Scalar.Red;

            Cv2.Line(boxImg, box[0], box[1], color, thickness);
            Cv2.Line(boxImg, box[1], box[2], color, thickness);
            Cv2.Line(boxImg, box[2], box[3], color, thickness);
            Cv2.Line(boxImg, box[3], box[0], color, thickness);
        }

        public static void DrawTextBoxes(Mat src, List<TextBox> textBoxes, int thickness)
        {
            for (int i = 0; i < textBoxes.Count; i++)
            {
                TextBox t = textBoxes[i];
                DrawTextBox(src, t.Points, thickness);
            }
        }

        public static List<Mat> GetPartImages(Mat src, List<TextBox> textBoxes)
        {
            List<Mat> partImages = new List<Mat>();
            for (int i = 0; i < textBoxes.Count; ++i)
            {
                Mat partImg = GetRotateCropImage(src, textBoxes[i].Points);
                partImages.Add(partImg);
            }
            return partImages;
        }

        public static Mat GetRotateCropImage(Mat src, List<Point> box)
        {
            List<Point> points = new List<Point>();
            points.AddRange(box);

            int[] collectX = { box[0].X, box[1].X, box[2].X, box[3].X };
            int[] collectY = { box[0].Y, box[1].Y, box[2].Y, box[3].Y };
            int left = collectX.Min();
            int right = collectX.Max();
            int top = collectY.Min();
            int bottom = collectY.Max();

            Rect rect = new Rect(left, top, right - left, bottom - top);
            Mat imgCrop = new Mat(src, rect);

            for (int i = 0; i < points.Count; i++)
            {
                var pt = points[i];
                pt.X -= left;
                pt.Y -= top;
                points[i] = pt;
            }

            int imgCropWidth = (int)(Math.Sqrt(Math.Pow(points[0].X - points[1].X, 2) +
                                        Math.Pow(points[0].Y - points[1].Y, 2)));
            int imgCropHeight = (int)(Math.Sqrt(Math.Pow(points[0].X - points[3].X, 2) +
                                         Math.Pow(points[0].Y - points[3].Y, 2)));

            var ptsDst0 = new Point2f(0, 0);
            var ptsDst1 = new Point2f(imgCropWidth, 0);
            var ptsDst2 = new Point2f(imgCropWidth, imgCropHeight);
            var ptsDst3 = new Point2f(0, imgCropHeight);

            Point2f[] ptsDst = { ptsDst0, ptsDst1, ptsDst2, ptsDst3 };


            var ptsSrc0 = new Point2f(points[0].X, points[0].Y);
            var ptsSrc1 = new Point2f(points[1].X, points[1].Y);
            var ptsSrc2 = new Point2f(points[2].X, points[2].Y);
            var ptsSrc3 = new Point2f(points[3].X, points[3].Y);

            Point2f[] ptsSrc = { ptsSrc0, ptsSrc1, ptsSrc2, ptsSrc3 };

            Mat M = Cv2.GetPerspectiveTransform(ptsSrc, ptsDst);

            Mat partImg = new Mat();

            Cv2.WarpPerspective(imgCrop, partImg, M, new OpenCvSharp.Size(imgCropWidth, imgCropHeight), InterpolationFlags.Nearest, BorderTypes.Replicate);

            if (partImg.Rows >= partImg.Cols * 1.5)
            {
                Mat srcCopy = new Mat();

                Cv2.Transpose(partImg, srcCopy);
                Cv2.Flip(srcCopy, srcCopy, 0);

                return srcCopy;
            }
            else
            {
                return partImg;
            }
        }

        public static Mat MatRotateClockWise180(Mat src)
        {
            Cv2.Flip(src, src, FlipMode.XY);

            return src;
        }

        public static Mat MatRotateClockWise90(Mat src)
        {
            Cv2.Rotate(src, src, RotateFlags.Rotate90Counterclockwise);
            return src;
        }

        /// <summary>
        /// Обрабатывает вертикальные и квадратные боксы: ищет пересекающиеся горизонтальные боксы и склеивает их в единые строки.
        /// </summary>
        public static List<TextBox> MergeVerticalWithHorizontalLines(List<TextBox> textBoxes)
        {
            if (textBoxes == null || textBoxes.Count == 0) return textBoxes;

            var result = new List<TextBox>();
            var horizontalBoxes = new List<TextBox>();
            var verticalBoxes = new List<TextBox>();
            var squareBoxes = new List<TextBox>(); // Квадратные и короткие боксы (например, отдельные цифры)

            // 1. Классифицируем боксы
            foreach (var box in textBoxes)
            {
                var rect = GetBoundingBox(box.Points);
                // Горизонтальные: ширина заметно больше высоты
                if (rect.Width > rect.Height * 2.0f)
                {
                    horizontalBoxes.Add(box);
                }
                // Вертикальные: высота заметно больше ширины
                else if (rect.Height > rect.Width * 1.5f)
                {
                    verticalBoxes.Add(box);
                }
                // Остальные (квадратные, короткие)
                else
                {
                    squareBoxes.Add(box);
                }
            }

            var mergedHBoxIndices = new HashSet<int>();
            var hBoxAttachments = new Dictionary<int, List<TextBox>>();
            for (int i = 0; i < horizontalBoxes.Count; i++) hBoxAttachments[i] = new List<TextBox>();

            // 2. Обрабатываем ВЕРТИКАЛЬНЫЕ боксы (нарезаем их по высоте горизонтальных строк)
            foreach (var vBox in verticalBoxes)
            {
                var vRect = GetBoundingBox(vBox.Points);
                var matchingHBoxes = new List<KeyValuePair<TextBox, int>>();

                for (int i = 0; i < horizontalBoxes.Count; i++)
                {
                    var hRect = GetBoundingBox(horizontalBoxes[i].Points);
                    int yOverlap = Math.Min(vRect.Bottom, hRect.Bottom) - Math.Max(vRect.Top, hRect.Top);

                    if (yOverlap > 0)
                    {
                        float overlapRatio = hRect.Height > 0 ? (float)yOverlap / hRect.Height : 0;
                        int xGap = Math.Max(0, Math.Max(vRect.Left, hRect.Left) - Math.Min(vRect.Right, hRect.Right));

                        // Кандидат должен быть преимущественно слева от текста или пересекаться с ним
                        bool isLeftOrOverlapping = vRect.Left < hRect.Left + hRect.Width * 0.5f;
                        int xGapTolerance = (int)Math.Max(hRect.Height * 20, 50); // Минимум 50px допуска

                        if (overlapRatio > 0.5f && xGap < xGapTolerance && isLeftOrOverlapping)
                        {
                            matchingHBoxes.Add(new KeyValuePair<TextBox, int>(horizontalBoxes[i], i));
                        }
                    }
                }

                if (matchingHBoxes.Count > 0)
                {
                    var sortedMatches = matchingHBoxes.OrderBy(x => GetBoundingBox(x.Key.Points).Top).ToList();

                    foreach (var match in sortedMatches)
                    {
                        mergedHBoxIndices.Add(match.Value);
                        var hBox = match.Key;
                        var hRect = GetBoundingBox(hBox.Points);

                        int newXMin = Math.Min(vRect.Left, hRect.Left);
                        int newXMax = Math.Max(vRect.Right, hRect.Right);
                        int newYMin = hRect.Top;
                        int newYMax = hRect.Bottom;

                        var newPoints = new List<Point>
                {
                    new Point(newXMin, newYMin),
                    new Point(newXMax, newYMin),
                    new Point(newXMax, newYMax),
                    new Point(newXMin, newYMax)
                };

                        result.Add(new TextBox
                        {
                            Points = newPoints,
                            Score = Math.Max(vBox.Score, hBox.Score)
                        });
                    }
                }
                else
                {
                    // Если вертикальный бокс ни с чем не пересекся, оставляем его как есть
                    result.Add(vBox);
                }
            }

            // 3. Обрабатываем КВАДРАТНЫЕ боксы (ищем строку по совпадению центра по Y)
            var usedSquareIndices = new HashSet<int>();
            for (int j = 0; j < squareBoxes.Count; j++)
            {
                var sBox = squareBoxes[j];
                var sRect = GetBoundingBox(sBox.Points);
                float sCenterY = sRect.Top + sRect.Height / 2.0f;

                TextBox bestMatchHBox = null;
                int bestMatchIndex = -1;
                float minDistance = float.MaxValue;

                for (int i = 0; i < horizontalBoxes.Count; i++)
                {
                    // Пропускаем строки, которые уже были "забраны" вертикальными боксами
                    if (mergedHBoxIndices.Contains(i)) continue;

                    var hRect = GetBoundingBox(horizontalBoxes[i].Points);
                    float hCenterY = hRect.Top + hRect.Height / 2.0f;
                    float yDiff = Math.Abs(sCenterY - hCenterY);

                    int xGap = Math.Max(0, Math.Max(hRect.Left, sRect.Left) - Math.Min(hRect.Right, sRect.Right));
                    bool isLeftOrOverlapping = sRect.Left < hRect.Left + hRect.Width * 0.5f;

                    // Допуски с защитой от слишком мелких шрифтов
                    float yTolerance = Math.Max(hRect.Height * 0.5f, 5.0f);
                    int xGapTolerance = (int)Math.Max(hRect.Height * 20, 50);

                    // Критерий совпадения для квадратного бокса
                    if (yDiff <= yTolerance && xGap < xGapTolerance && isLeftOrOverlapping)
                    {
                        if (yDiff < minDistance)
                        {
                            minDistance = yDiff;
                            bestMatchHBox = horizontalBoxes[i];
                            bestMatchIndex = i;
                        }
                    }
                }

                if (bestMatchHBox != null)
                {
                    hBoxAttachments[bestMatchIndex].Add(sBox);
                    usedSquareIndices.Add(j);
                }
            }

            // 4. Собираем оставшиеся горизонтальные боксы с их квадратными привязками
            for (int i = 0; i < horizontalBoxes.Count; i++)
            {
                if (mergedHBoxIndices.Contains(i)) continue;

                var hBox = horizontalBoxes[i];
                var attachments = hBoxAttachments[i];

                if (attachments.Count > 0)
                {
                    var hRect = GetBoundingBox(hBox.Points);
                    int newXMin = hRect.Left;
                    int newXMax = hRect.Right;
                    int newYMin = hRect.Top;
                    int newYMax = hRect.Bottom;

                    float maxScore = hBox.Score;

                    foreach (var sBox in attachments)
                    {
                        var sRect = GetBoundingBox(sBox.Points);
                        newXMin = Math.Min(newXMin, sRect.Left); // Расширяем бокс влево, захватывая цифры
                        newYMin = Math.Min(newYMin, sRect.Top);
                        newYMax = Math.Max(newYMax, sRect.Bottom);
                        if (sBox.Score > maxScore) maxScore = sBox.Score;
                    }

                    var newPoints = new List<Point>
            {
                new Point(newXMin, newYMin),
                new Point(newXMax, newYMin),
                new Point(newXMax, newYMax),
                new Point(newXMin, newYMax)
            };

                    result.Add(new TextBox
                    {
                        Points = newPoints,
                        Score = maxScore
                    });
                }
                else
                {
                    result.Add(hBox);
                }
            }

            // 5. Добавляем непривязанные квадратные боксы (шум или одиночные символы)
            for (int j = 0; j < squareBoxes.Count; j++)
            {
                if (!usedSquareIndices.Contains(j))
                {
                    result.Add(squareBoxes[j]);
                }
            }

            // Сортируем итоговый список боксов (сверху-вниз, затем слева-направо) для сохранения порядка чтения
            result = result.OrderBy(b => GetBoundingBox(b.Points).Top)
                           .ThenBy(b => GetBoundingBox(b.Points).Left)
                           .ToList();

            return result;
        }

        /// <summary>
        /// Возвращает ограничивающий прямоугольник (Bounding Box) для списка точек
        /// </summary>
        public static Rect GetBoundingBox(List<Point> points)
        {
            int minX = points.Min(p => p.X);
            int maxX = points.Max(p => p.X);
            int minY = points.Min(p => p.Y);
            int maxY = points.Max(p => p.Y);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
