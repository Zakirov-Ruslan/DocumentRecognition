using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DocumentRecognition.OnnxRecognizer
{
    public class DrawingLayoutDetection
    {
        public string Label { get; set; } = string.Empty;
        public float Score { get; set; }
        public Rect Box { get; set; }
    }

    public class DrawingLayoutDetector : IDisposable, IInferenceEngine
    {
        private readonly Lazy<InferenceSession> _session;
        private readonly string _modelPath;
        private readonly InferenceExecutionProvider _provider;
        private readonly int _threads;

        private readonly string[] _labels = {
            "technical_requirements", "view",  "general_roughness",
            "title_block_2", "additional_graphs_left", "configuration_table", "title_block_2a",
            "parameter_table", "holes_table"
        };

        private const int InputH = 640;
        private const int InputW = 640;
        private const float ScoreThreshold = 0.3f;

        public DrawingLayoutDetector(
            string onnxPath,
            InferenceExecutionProvider provider = InferenceExecutionProvider.Auto,
            int threads = 0)
        {
            // Конструктор дешёвый: модель грузится лениво при первом инференсе
            // или при явном вызове InitializeAsync() — как в OcrLite.InitModels().
            _modelPath = onnxPath;
            _provider = provider;
            _threads = threads;
            _session = new Lazy<InferenceSession>(CreateSession, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <inheritdoc />
        public bool IsInitialized => _session.IsValueCreated;

        /// <summary>
        /// Прогрев модели: загрузка выполняется в пуле потоков, идемпотентно
        /// и потокобезопасно (повторные вызовы возвращают тот же результат).
        /// </summary>
        public Task InitializeAsync()
        {
            if (_session.IsValueCreated)
                return Task.CompletedTask;

            return Task.Run(() => _session.Value);
        }

        private InferenceSession CreateSession() =>
            OnnxSessionFactory.Create(
                _modelPath,
                nameof(DrawingLayoutDetector),
                _provider,
                _threads,
                GraphOptimizationLevel.ORT_ENABLE_EXTENDED);

        private InferenceSession Session => _session.Value;

        public List<DrawingLayoutDetection> Detect(Mat image)
        {
            var session = Session;

            int origH = image.Rows;
            int origW = image.Cols;

            // --- Preprocess ---
            using (var resized = new Mat())
            {
                Cv2.Resize(image, resized, new Size(InputW, InputH));
                Cv2.CvtColor(resized, resized, ColorConversionCodes.BGR2RGB);

                float[] inputData = new float[3 * InputH * InputW];

                for (int c = 0; c < 3; c++)
                {
                    for (int h = 0; h < InputH; h++)
                    {
                        for (int w = 0; w < InputW; w++)
                        {
                            float pixel = resized.At<Vec3b>(h, w)[c] / 255f;
                            inputData[c * InputH * InputW + h * InputW + w] = pixel;
                        }
                    }
                }

                var imageTensor = new DenseTensor<float>(inputData, new[] { 1, 3, InputH, InputW });

                var inputs = new List<NamedOnnxValue>();
                foreach (var inputMeta in session.InputMetadata)
                {
                    if (inputMeta.Value.Dimensions.Length == 4)
                    {
                        inputs.Add(NamedOnnxValue.CreateFromTensor(inputMeta.Key, imageTensor));
                    }
                    else if (inputMeta.Value.Dimensions.Length == 2)
                    {
                        var scaleTensor = new DenseTensor<float>(new float[] {
                        (float)origW / InputW,
                        (float)origH / InputH
                    }, new[] { 1, 2 });

                        inputs.Add(NamedOnnxValue.CreateFromTensor(inputMeta.Key, scaleTensor));
                    }
                }

                // --- Inference ---
                using (var results = session.Run(inputs))
                {
                    var outputTensor = results.First().AsTensor<float>();
                    var output = outputTensor.ToArray();
                    var shape = outputTensor.Dimensions;

                    // --- Postprocess ---
                    int numBoxes = output.Length / 6;

                    var detections = new List<DrawingLayoutDetection>();

                    for (int i = 0; i < numBoxes; i++)
                    {
                        int offset = i * 6;
                        if (offset + 5 >= output.Length) break;

                        // Format: [class_id, score, x1, y1, x2, y2]
                        int classId = (int)output[offset + 0];
                        float score = output[offset + 1];

                        if (score < ScoreThreshold || classId < 0 || classId >= _labels.Length)
                            continue;

                        float x1_norm = output[offset + 2];
                        float y1_norm = output[offset + 3];
                        float x2_norm = output[offset + 4];
                        float y2_norm = output[offset + 5];


                        int x1 = (int)(x1_norm * origW);
                        int y1 = (int)(y1_norm * origH);
                        int x2 = (int)(x2_norm * origW);
                        int y2 = (int)(y2_norm * origH);

                        x1 = Math.Max(0, Math.Min(origW - 1, x1));
                        y1 = Math.Max(0, Math.Min(origH - 1, y1));
                        x2 = Math.Max(0, Math.Min(origW - 1, x2));
                        y2 = Math.Max(0, Math.Min(origH - 1, y2));

                        if (x2 <= x1 || y2 <= y1)
                            continue;

                        detections.Add(new DrawingLayoutDetection
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

        public Mat DrawDetections(Mat srcMat, List<DrawingLayoutDetection> detections)
        {
            foreach (var det in detections)
            {
                Console.WriteLine($"  [{det.Label}] score={det.Score:F3} box={det.Box}");

                Cv2.Rectangle(srcMat, det.Box, new Scalar(0, 255, 0), 2);
                string text = $"{det.Label} {det.Score:F2}";
                Cv2.PutText(srcMat, text, new Point(det.Box.X, det.Box.Y - 5),
                    HersheyFonts.HersheySimplex, 0.5, new Scalar(0, 0, 255), 1);
            }

            return srcMat;
        }

        public void Dispose()
        {
            // Сессия могла так и не создаться — тогда освобождать нечего.
            if (_session.IsValueCreated)
                _session.Value.Dispose();
        }
    }
}
