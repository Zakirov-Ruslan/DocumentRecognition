/*
 * Modified by Zakirov-Ruslan
 * Original: https://github.com/RapidAI/RapidOCRCSharp
 */

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Ocr.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ocr
{
    internal class TextRecognizer : IDisposable
    {
        private readonly float[] _meanValues = { 127.5F, 127.5F, 127.5F };
        private readonly float[] _normValues = { 1.0F / 127.5F, 1.0F / 127.5F, 1.0F / 127.5F };
        private const int _crnnDstHeight = 48;

        private InferenceSession _crnnNet;
        private List<string> _keys;
        private List<string> _inputNames;

        internal TextRecognizer() { }

        ~TextRecognizer()
        {
            _crnnNet?.Dispose();
        }

        public async Task InitModel(string path, string keysPath, int numThread)
        {
            try
            {
                SessionOptions op = new SessionOptions();

                op.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED;

                try
                {
                    op.AppendExecutionProvider_DML();
                    Console.WriteLine($"[CrnnNet] DirectML initialized on device");
                }
                catch (Exception dmlEx)
                {
                    // Fallback CPU, if DirectML now awailable
                    Console.WriteLine($"[CrnnNet] DirectML failed ({dmlEx.Message}), falling back to CPU.");

                    op.AppendExecutionProvider_CPU(0);

                    op.InterOpNumThreads = numThread;
                    op.IntraOpNumThreads = numThread;
                }

                _crnnNet = new InferenceSession(path, op);
                _inputNames = _crnnNet.InputMetadata.Keys.ToList();
                _keys = InitKeys(keysPath);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                throw;
            }
        }
        private List<string> InitKeys(string path)
        {
            StreamReader sr = new StreamReader(path, Encoding.UTF8);
            List<string> keys = new List<string>();
            keys.Add("#");
            String line;
            while ((line = sr.ReadLine()) != null)
            {
                //Console.WriteLine(line.ToString());
                keys.Add(line);
            }
            keys.Add(" ");
            Console.WriteLine($"keys Size = {keys.Count}");
            return keys;
        }

        public List<TextLine> GetTextLines(List<Mat> partImgs)
        {
            List<TextLine> textLines = new List<TextLine>();
            for (int i = 0; i < partImgs.Count; i++)
            {
                var startTicks = DateTime.Now.Ticks;
                var textLine = GetTextLine(partImgs[i]);
                var endTicks = DateTime.Now.Ticks;
                var crnnTime = (endTicks - startTicks) / 10000F;
                textLine.Time = crnnTime;
                textLines.Add(textLine);
            }
            return textLines;
        }

        public List<TextLine> GetTextLinesBatched(List<Mat> partImgs, int batchSize = 16)
        {
            List<TextLine> textLines = new List<TextLine>();
            var startTicks = DateTime.Now.Ticks;

            for (int startIdx = 0; startIdx < partImgs.Count; startIdx += batchSize)
            {
                int currentBatchSize = Math.Min(batchSize, partImgs.Count - startIdx);
                var batchImgs = partImgs.GetRange(startIdx, currentBatchSize);

                var resizedImgs = new List<Mat>();
                int maxWidth = 0;

                foreach (var img in batchImgs)
                {
                    int dstWidth = (int)(img.Cols * (_crnnDstHeight / (float)img.Rows));
                    maxWidth = Math.Max(maxWidth, dstWidth);

                    Mat resized = new Mat();
                    Cv2.Resize(img, resized, new Size(dstWidth, _crnnDstHeight));
                    resizedImgs.Add(resized);
                }

                var batchTensor = new DenseTensor<float>(new[] { currentBatchSize, 3, _crnnDstHeight, maxWidth });

                for (int b = 0; b < currentBatchSize; b++)
                {
                    using (var img = resizedImgs[b])
                    {
                        var normalized = OcrUtils.SubstractMeanNormalize(img, _meanValues, _normValues);
                        int actualWidth = img.Cols;

                        for (int c = 0; c < 3; c++)
                            for (int h = 0; h < _crnnDstHeight; h++)
                                for (int w = 0; w < actualWidth; w++)
                                {
                                    batchTensor[b, c, h, w] = normalized[0, c, h, w];
                                }
                    }
                }

                foreach (var img in resizedImgs) 
                    img.Dispose();

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(_inputNames[0], batchTensor)
                };

                try
                {
                    using (var results = _crnnNet.Run(inputs))
                    {
                        var resultsArray = results.ToArray();
                        var outputTensor = resultsArray[0].AsTensor<float>();
                        var dims = outputTensor.Dimensions; // [batch, seq_len, num_classes]
                        float[] outputData = outputTensor.ToArray();

                        int seqLen = dims[1];
                        int numClasses = dims[2];

                        for (int b = 0; b < currentBatchSize; b++)
                        {
                            float[] slice = new float[seqLen * numClasses];
                            Array.Copy(outputData, b * slice.Length, slice, 0, slice.Length);
                            textLines.Add(ScoreToTextLine(slice, seqLen, numClasses));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CrnnNet] Batch inference failed: {ex.Message}");
                    // Fallback 
                    for (int b = 0; b < currentBatchSize; b++)
                    {
                        textLines.Add(GetTextLine(partImgs[startIdx + b]));
                    }
                }
            }

            var totalMs = (DateTime.Now.Ticks - startTicks) / 10000F;
            var perImgMs = totalMs / partImgs.Count;
            foreach (var tl in textLines) tl.Time = perImgMs;

            return textLines;
        }

        private TextLine GetTextLine(Mat src)
        {
            TextLine textLine = new TextLine();

            float scale = (float)_crnnDstHeight / (float)src.Rows;
            int dstWidth = (int)((float)src.Cols * scale);

            Mat srcResize = new Mat();
            Cv2.Resize(src, srcResize, new Size(dstWidth, _crnnDstHeight));

            Tensor<float> inputTensors = OcrUtils.SubstractMeanNormalize(srcResize, _meanValues, _normValues);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputNames[0], inputTensors)
            };
            try
            {
                using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _crnnNet.Run(inputs))
                {
                    var resultsArray = results.ToArray();
                    var dimensions = resultsArray[0].AsTensor<float>().Dimensions;
                    float[] outputData = resultsArray[0].AsEnumerable<float>().ToArray();

                    return ScoreToTextLine(outputData, dimensions[1], dimensions[2]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                throw;
            }

            return textLine;
        }

        private TextLine ScoreToTextLine(float[] srcData, int h, int w)
        {
            StringBuilder sb = new StringBuilder();
            TextLine textLine = new TextLine();

            int lastIndex = 0;
            List<float> scores = new List<float>();

            for (int i = 0; i < h; i++)
            {
                int maxIndex = 0;
                float maxValue = -1000F;
                for (int j = 0; j < w; j++)
                {
                    int idx = i * w + j;
                    if (srcData[idx] > maxValue)
                    {
                        maxIndex = j;
                        maxValue = srcData[idx];
                    }
                }

                if (maxIndex > 0 && maxIndex < _keys.Count && (!(i > 0 && maxIndex == lastIndex)))
                {
                    scores.Add(maxValue);
                    sb.Append(_keys[maxIndex]);
                }
                lastIndex = maxIndex;
            }

            textLine.Text = sb.ToString();
            textLine.CharScores = scores;

            return textLine;
        }

        public void Dispose()
        {
            _crnnNet?.Dispose();
        }
    }
}
