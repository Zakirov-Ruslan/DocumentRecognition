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
using System.Linq;
using System.Threading.Tasks;

using Size = OpenCvSharp.Size;

namespace Ocr
{
    internal class TextLineOrientationClassifier : IDisposable
    {
        private readonly float[] _meanValues = { 127.5F, 127.5F, 127.5F };
        private readonly float[] _normValues = { 1.0F / 127.5F, 1.0F / 127.5F, 1.0F / 127.5F };
        private const int _angleCols = 2;
        private InferenceSession _angleNet;
        private List<string> _inputNames;

        internal TextLineOrientationClassifier() { }

        ~TextLineOrientationClassifier()
        {
            _angleNet.Dispose();
        }

        public async Task InitModel(string path, int numThread)
        {
            try
            {
                SessionOptions op = new SessionOptions();
                op.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED;

                try
                {
                    op.AppendExecutionProvider_DML();
                    Console.WriteLine($"[AngleNet] DirectML initialized on device");
                }
                catch (Exception dmlEx)
                {
                    // Fallback CPU, if DirectML now awailable
                    Console.WriteLine($"[AngleNet] DirectML failed ({dmlEx.Message}), falling back to CPU.");

                    op.AppendExecutionProvider_CPU(0);

                    op.InterOpNumThreads = numThread;
                    op.IntraOpNumThreads = numThread;
                }

                _angleNet = new InferenceSession(path, op);
                _inputNames = _angleNet.InputMetadata.Keys.ToList();

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                throw;
            }
        }

        public List<Angle> GetAngles(List<Mat> partImgs, bool doAngle, bool mostAngle)
        {
            List<Angle> angles = new List<Angle>();
            if (doAngle)
            {
                for (int i = 0; i < partImgs.Count; i++)
                {
                    var startTicks = DateTime.Now.Ticks;
                    var angle = GetAngle(partImgs[i]);
                    var endTicks = DateTime.Now.Ticks;
                    var angleTime = (endTicks - startTicks) / 10000F;
                    angle.Time = angleTime;
                    angles.Add(angle);
                }
            }
            else
            {
                for (int i = 0; i < partImgs.Count; i++)
                {
                    var angle = new Angle();
                    angle.Index = -1;
                    angle.Score = 0F;
                    angles.Add(angle);
                }
            }

            // Most Possible AngleIndex
            if (doAngle && mostAngle)
            {
                List<int> angleIndexes = new List<int>();
                angles.ForEach(x => angleIndexes.Add(x.Index));

                double sum = angleIndexes.Sum();
                double halfPercent = angles.Count / 2.0f;
                int mostAngleIndex;
                // All angle set to 0
                if (sum < halfPercent)
                {
                    mostAngleIndex = 0;
                }
                // All angle set to 1
                else
                {
                    mostAngleIndex = 1;
                }
                Console.WriteLine($"Set All Angle to mostAngleIndex({mostAngleIndex})");
                for (int i = 0; i < angles.Count; ++i)
                {
                    Angle angle = angles[i];
                    angle.Index = mostAngleIndex;
                    angles[i] = angle;
                }
            }
            return angles;
        }

        public List<Angle> GetAnglesBatched(List<Mat> partImgs, bool doAngle, bool mostAngle, int batchSize = 16)
        {
            List<Angle> angles = new List<Angle>();

            if (!doAngle)
            {
                for (int i = 0; i < partImgs.Count; i++)
                {
                    angles.Add(new Angle { Index = -1, Score = 0F });
                }
                return angles;
            }

            var startTicks = DateTime.Now.Ticks;

            for (int startIdx = 0; startIdx < partImgs.Count; startIdx += batchSize)
            {
                int currentBatchSize = Math.Min(batchSize, partImgs.Count - startIdx);

                var batchTensor = new DenseTensor<float>(new[] { currentBatchSize, 3, 48, 192 });

                for (int b = 0; b < currentBatchSize; b++)
                {
                    using (Mat resized = new Mat())
                    {
                        Cv2.Resize(partImgs[startIdx + b], resized, new Size(192, 48));
                        var normalized = OcrUtils.SubstractMeanNormalize(resized, _meanValues, _normValues);

                        for (int c = 0; c < 3; c++)
                            for (int h = 0; h < 48; h++)
                                for (int w = 0; w < 192; w++)
                                {
                                    batchTensor[b, c, h, w] = normalized[0, c, h, w];
                                }
                    }
                }

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(_inputNames[0], batchTensor)
                };

                try
                {
                    using (var results = _angleNet.Run(inputs))
                    {
                        var resultsArray = results.ToArray();
                        var outputTensor = resultsArray[0].AsTensor<float>();
                        var dims = outputTensor.Dimensions;
                        float[] outputData = outputTensor.ToArray();


                        for (int b = 0; b < currentBatchSize; b++)
                        {
                            int sliceSize = dims[1]; // _angleCols = 2
                            float[] slice = new float[sliceSize];
                            for (int i = 0; i < sliceSize; i++)
                            {
                                slice[i] = outputData[b * sliceSize + i];
                            }
                            angles.Add(ScoreToAngle(slice, _angleCols));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AngleNet] Batch inference failed: {ex.Message}");
                    // Fallback
                    for (int b = 0; b < currentBatchSize; b++)
                    {
                        angles.Add(GetAngle(partImgs[startIdx + b]));
                    }
                }
            }

            // Time calcs
            var totalMs = (DateTime.Now.Ticks - startTicks) / 10000F;
            var perImgMs = totalMs / partImgs.Count;
            foreach (var angle in angles)
            {
                angle.Time = perImgMs;
            }

            // Most Possible AngleIndex
            if (mostAngle)
            {
                List<int> angleIndexes = angles.Select(x => x.Index).ToList();
                double sum = angleIndexes.Sum();
                int mostAngleIndex = sum < angles.Count / 2.0 ? 0 : 1;
                for (int i = 0; i < angles.Count; ++i)
                {
                    angles[i] = new Angle { Index = mostAngleIndex, Score = angles[i].Score, Time = angles[i].Time };
                }
            }

            return angles;
        }

        private Angle GetAngle(Mat src)
        {
            Angle angle = new Angle();

            Mat mat = new Mat();
            Cv2.Resize(src, mat, new Size(192, 48));

            Tensor<float> inputTensors = OcrUtils.SubstractMeanNormalize(mat, _meanValues, _normValues);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputNames[0], inputTensors)
            };
            try
            {
                using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _angleNet.Run(inputs))
                {
                    var resultsArray = results.ToArray();
                    Console.WriteLine(resultsArray);
                    float[] outputData = resultsArray[0].AsEnumerable<float>().ToArray();
                    return ScoreToAngle(outputData, _angleCols);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                //throw ex;
            }
            return angle;
        }

        private Angle ScoreToAngle(float[] srcData, int angleCols)
        {
            Angle angle = new Angle();
            int angleIndex = 0;
            float maxValue = -1000.0F;
            for (int i = 0; i < angleCols; i++)
            {
                if (i == 0) maxValue = srcData[i];
                else if (srcData[i] > maxValue)
                {
                    angleIndex = i;
                    maxValue = srcData[i];
                }
            }
            angle.Index = angleIndex;
            angle.Score = maxValue;
            return angle;
        }

        private Mat AdjustTargetImg(Mat src, int dstWidth, int dstHeight)
        {
            Mat srcResize = new Mat();
            float scale = (float)dstHeight / (float)src.Rows;
            int angleWidth = (int)((float)src.Cols * scale);

            Cv2.Resize(src, srcResize, new Size(angleWidth, dstHeight));

            Mat srcFit = new Mat(dstHeight, dstWidth, MatType.CV_8UC3, new Scalar(0, 0, 0));

            if (angleWidth < dstWidth)
            {
                Cv2.CopyMakeBorder(srcResize, srcFit, 0, 0, 0, dstWidth - angleWidth, BorderTypes.Isolated, Scalar.Black);
            }
            else
            {
                Rect rect = new Rect(0, 0, dstWidth, dstHeight);
                Mat partAngle = new Mat(srcResize, rect);
                partAngle.CopyTo(srcFit);
            }
            return srcFit;
        }

        public void Dispose()
        {
            _angleNet.Dispose();
        }
    }
}
