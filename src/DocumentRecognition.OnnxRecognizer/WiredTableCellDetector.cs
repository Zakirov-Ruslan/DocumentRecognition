using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DocumentRecognition.OnnxRecognizer
{
    public class TableCellDetection
    {
        public string Label { get; set; } = "cell";
        public float Score { get; set; }
        public Rect Box { get; set; }
    }

    public class WiredTableCellDetector : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string[] _labels = { "cell" };

        private const int InputH = 640;
        private const int InputW = 640;
        private const float ScoreThreshold = 0.3f;

        public WiredTableCellDetector(string onnxPath)
        {
            var op = new SessionOptions();
            op.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED;

            try
            {
                op.AppendExecutionProvider_DML();
                Console.WriteLine($"[WiredTableCellDetector] DirectML initialized");
            }
            catch (Exception dmlEx)
            {
                Console.WriteLine($"[WiredTableCellDetector] DirectML failed ({dmlEx.Message}), falling back to CPU.");
                op.AppendExecutionProvider_CPU(0);
            }

            _session = new InferenceSession(onnxPath, op);

            Console.WriteLine("[WiredTableCellDetector] Model inputs:");
            foreach (var input in _session.InputMetadata)
            {
                Console.WriteLine($"  {input.Key}: {string.Join("x", input.Value.Dimensions)}");
            }

            Console.WriteLine("[WiredTableCellDetector] Model outputs:");
            foreach (var output in _session.OutputMetadata)
            {
                Console.WriteLine($"  {output.Key}: {string.Join("x", output.Value.Dimensions)}");
            }
        }

        public List<TableCellDetection> Detect(Mat image)
        {
            int origH = image.Rows;
            int origW = image.Cols;

            using (var resized = new Mat())
            {
                // Resize WITHOUT keeping aspect ratio (keep_ratio: false)
                Cv2.Resize(image, resized, new Size(InputW, InputH));
                Cv2.CvtColor(resized, resized, ColorConversionCodes.BGR2RGB);

                // Нормализация для RT-DETR: [0, 255] -> [0, 1]
                float[] inputData = new float[3 * InputH * InputW];

                for (int c = 0; c < 3; c++)
                {
                    for (int h = 0; h < InputH; h++)
                    {
                        for (int w = 0; w < InputW; w++)
                        {
                            // Нормализуем в [0, 1]
                            inputData[c * InputH * InputW + h * InputW + w] = resized.At<Vec3b>(h, w)[c] / 255f;
                        }
                    }
                }

                var imageTensor = new DenseTensor<float>(inputData, new[] { 1, 3, InputH, InputW });

                var inputs = new List<NamedOnnxValue>();
                foreach (var inputMeta in _session.InputMetadata)
                {
                    if (inputMeta.Value.Dimensions.Length == 4)
                    {
                        // image input
                        inputs.Add(NamedOnnxValue.CreateFromTensor(inputMeta.Key, imageTensor));
                    }
                    else if (inputMeta.Value.Dimensions.Length == 2)
                    {
                        if (inputMeta.Key.ToLower().Contains("shape"))
                        {
                            // im_shape: [origH, origW]
                            var shapeTensor = new DenseTensor<float>(new float[] { origH, origW }, new[] { 1, 2 });
                            inputs.Add(NamedOnnxValue.CreateFromTensor(inputMeta.Key, shapeTensor));
                        }
                        else if (inputMeta.Key.ToLower().Contains("scale"))
                        {
                            // scale_factor: [scale_h, scale_w] 
                            var scaleTensor = new DenseTensor<float>(new float[] {
                                (float)origH / InputH,
                                (float)origW / InputW
                            }, new[] { 1, 2 });
                            inputs.Add(NamedOnnxValue.CreateFromTensor(inputMeta.Key, scaleTensor));
                        }
                        else
                        {
                            // Fallback: пытаемся угадать
                            var fallbackTensor = new DenseTensor<float>(new float[] {
                                (float)origH / InputH,
                                (float)origW / InputW
                            }, new[] { 1, 2 });
                            inputs.Add(NamedOnnxValue.CreateFromTensor(inputMeta.Key, fallbackTensor));
                        }
                    }
                }

                using (var results = _session.Run(inputs))
                {
                    var outputTensor = results.First().AsTensor<float>();
                    var output = outputTensor.ToArray();
                    var shape = outputTensor.Dimensions;

                    // RT-DETR экспортированный из PaddleDetection обычно имеет формат [1, 6, N]
                    bool is6N = false;
                    int numBoxes = 0;

                    if (shape.Length >= 2)
                    {
                        // Проверяем формат [1, 6, N]
                        if (shape[shape.Length - 2] == 6 && shape[shape.Length - 1] > 0)
                        {
                            is6N = true;
                            numBoxes = shape[shape.Length - 1];
                        }
                        else if (shape[shape.Length - 1] == 6)
                        {
                            is6N = false;
                            numBoxes = shape[shape.Length - 2];
                        }
                    }

                    if (numBoxes == 0)
                    {
                        numBoxes = output.Length / 6;
                    }

                    var detections = new List<TableCellDetection>();

                    // Для RT-DETR координаты ВСЕГДА нормализованы [0, 1] относительно исходного изображения
                    // или относительно 640x640 в зависимости от модели
                    // Проверим несколько первых детекций, чтобы определить формат
                    bool coordsNormalized = false;
                    bool coordsIn640 = false;

                    if (numBoxes > 0)
                    {
                        float maxCoord = 0;
                        float minCoord = float.MaxValue;

                        for (int i = 0; i < Math.Min(numBoxes, 10); i++)
                        {
                            float x1 = 0, y1 = 0, x2 = 0, y2 = 0;

                            if (is6N)
                            {
                                x1 = output[2 * numBoxes + i];
                                y1 = output[3 * numBoxes + i];
                                x2 = output[4 * numBoxes + i];
                                y2 = output[5 * numBoxes + i];
                            }
                            else
                            {
                                int offset = i * 6;
                                if (offset + 5 < output.Length)
                                {
                                    x1 = output[offset + 2];
                                    y1 = output[offset + 3];
                                    x2 = output[offset + 4];
                                    y2 = output[offset + 5];
                                }
                            }

                            if (x1 != 0 || y1 != 0 || x2 != 0 || y2 != 0)
                            {
                                maxCoord = Math.Max(maxCoord, Math.Max(x1, Math.Max(y1, Math.Max(x2, y2))));
                                minCoord = Math.Min(minCoord, Math.Min(x1, Math.Min(y1, Math.Min(x2, y2))));
                            }
                        }

                        coordsNormalized = maxCoord <= 1.1f;
                        coordsIn640 = !coordsNormalized && maxCoord <= InputW && maxCoord <= InputH;
                    }

                    for (int i = 0; i < numBoxes; i++)
                    {
                        int classId = 0;
                        float score = 0;
                        float x1_val = 0, y1_val = 0, x2_val = 0, y2_val = 0;

                        if (is6N)
                        {
                            // Формат [1, 6, N]
                            classId = (int)output[0 * numBoxes + i];
                            score = output[1 * numBoxes + i];
                            x1_val = output[2 * numBoxes + i];
                            y1_val = output[3 * numBoxes + i];
                            x2_val = output[4 * numBoxes + i];
                            y2_val = output[5 * numBoxes + i];
                        }
                        else
                        {
                            // Формат [1, N, 6]
                            int offset = i * 6;
                            if (offset + 5 >= output.Length) break;
                            classId = (int)output[offset + 0];
                            score = output[offset + 1];
                            x1_val = output[offset + 2];
                            y1_val = output[offset + 3];
                            x2_val = output[offset + 4];
                            y2_val = output[offset + 5];
                        }

                        if (score < ScoreThreshold)
                            continue;

                        if (classId < 0 || classId >= _labels.Length)
                            continue;

                        int x1, y1, x2, y2;

                        // Масштабируем координаты
                        if (coordsNormalized)
                        {
                            // Нормализованные [0, 1] -> масштабируем к оригинальному размеру
                            x1 = (int)(x1_val * origW);
                            y1 = (int)(y1_val * origH);
                            x2 = (int)(x2_val * origW);
                            y2 = (int)(y2_val * origH);
                        }
                        else if (coordsIn640)
                        {
                            // Координаты в 640x640 -> масштабируем к оригинальному размеру
                            x1 = (int)(x1_val * origW / InputW);
                            y1 = (int)(y1_val * origH / InputH);
                            x2 = (int)(x2_val * origW / InputW);
                            y2 = (int)(y2_val * origH / InputH);
                        }
                        else
                        {
                            // Если координаты больше 640, возможно уже в исходных пикселях
                            if (x1_val > 1000 || y1_val > 1000)
                            {
                                // Уже в исходных пикселях
                                x1 = (int)x1_val;
                                y1 = (int)y1_val;
                                x2 = (int)x2_val;
                                y2 = (int)y2_val;
                            }
                            else
                            {
                                // Fallback: считаем что в 640x640
                                x1 = (int)(x1_val * origW / InputW);
                                y1 = (int)(y1_val * origH / InputH);
                                x2 = (int)(x2_val * origW / InputW);
                                y2 = (int)(y2_val * origH / InputH);
                            }
                        }

                        // Нормализуем координаты (x1 < x2, y1 < y2)
                        if (x1 > x2) { int tmp = x1; x1 = x2; x2 = tmp; }
                        if (y1 > y2) { int tmp = y1; y1 = y2; y2 = tmp; }

                        // Clamp к границам изображения
                        x1 = Math.Max(0, Math.Min(origW - 1, x1));
                        y1 = Math.Max(0, Math.Min(origH - 1, y1));
                        x2 = Math.Max(0, Math.Min(origW - 1, x2));
                        y2 = Math.Max(0, Math.Min(origH - 1, y2));

                        if (x2 <= x1 || y2 <= y1)
                            continue;

                        detections.Add(new TableCellDetection
                        {
                            Label = _labels[classId],
                            Score = score,
                            Box = new Rect(x1, y1, x2 - x1, y2 - y1)
                        });
                    }

                    return detections;
                }
            }
        }

        public Mat DrawDetections(Mat srcMat, List<TableCellDetection> detections)
        {
            foreach (var det in detections)
            {
                Console.WriteLine($"  [{det.Label}] score={det.Score:F3} box={det.Box}");

                Cv2.Rectangle(srcMat, det.Box, new Scalar(0, 255, 0), 2);
            }

            return srcMat;
        }

        public void Dispose() => _session?.Dispose();
    }
}