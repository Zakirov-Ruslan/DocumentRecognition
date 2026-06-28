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
    internal class AngleNet
    {
        private readonly float[] _meanValues = { 127.5F, 127.5F, 127.5F };
        private readonly float[] _normValues = { 1.0F / 127.5F, 1.0F / 127.5F, 1.0F / 127.5F };
        private const int _angleCols = 2;
        private InferenceSession _angleNet;
        private List<string> _inputNames;

        internal AngleNet() { }

        ~AngleNet()
        {
            _angleNet.Dispose();
        }

        public async Task InitModel(string path, int numThread)
        {
            try
            {
                SessionOptions op = new SessionOptions();
                op.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED;
                op.InterOpNumThreads = numThread;
                op.IntraOpNumThreads = numThread;
                _angleNet = new InferenceSession(path, op);
                _inputNames = _angleNet.InputMetadata.Keys.ToList();

                var dims = _angleNet.InputMetadata.First().Value.Dimensions;

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

    }
}
