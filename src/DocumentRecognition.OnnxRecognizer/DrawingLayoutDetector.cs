using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DocumentRecognition.OnnxRecognizer
{
    public class DrawingLayoutDetection
    {
        public string Label { get; set; } = string.Empty;
        public float Score { get; set; }
        public Rect Box { get; set; }
    }

    public class DrawingLayoutDetector : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string[] _labels = {
            "technical_requirements", "view",  "general_roughness",
            "title_block_2", "additional_graphs_left", "configuration_table", "title_block_2a",
            "parameter_table", "holes_table"
        };

        private const int InputH = 640;
        private const int InputW = 640;
        private const float ScoreThreshold = 0.3f;

        public DrawingLayoutDetector(string onnxPath)
        {
            var opts = new SessionOptions();
            opts.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            _session = new InferenceSession(onnxPath, opts);
        }

        public List<DrawingLayoutDetection> Detect(Mat image)
        {
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
                foreach (var inputMeta in _session.InputMetadata)
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
                using (var results = _session.Run(inputs))
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

        public byte[] DrawDetections(Mat srcMat, List<DrawingLayoutDetection> detections)
        {
            foreach (var det in detections)
            {
                Console.WriteLine($"  [{det.Label}] score={det.Score:F3} box={det.Box}");

                Cv2.Rectangle(srcMat, det.Box, new Scalar(0, 255, 0), 2);
                string text = $"{det.Label} {det.Score:F2}";
                Cv2.PutText(srcMat, text, new Point(det.Box.X, det.Box.Y - 5),
                    HersheyFonts.HersheySimplex, 0.5, new Scalar(0, 0, 255), 1);
            }

            return srcMat.ToBytes();
        }

        public void Dispose() => _session?.Dispose();
    }
}
