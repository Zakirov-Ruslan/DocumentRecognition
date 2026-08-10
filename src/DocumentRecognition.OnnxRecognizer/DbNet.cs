/*
 * Modified by Zakirov-Ruslan
 * Original: https://github.com/RapidAI/RapidOCRCSharp
 */

using Clipper2Lib;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Ocr.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Point = OpenCvSharp.Point;
using Point2f = OpenCvSharp.Point2f;
using Size = OpenCvSharp.Size;
using TextBox = Ocr.Models.TextBox;

namespace Ocr
{
    internal class DbNet : IDisposable
    {
        private readonly float[] _meanValues = { 0.485F * 255F, 0.456F * 255F, 0.406F * 255F };
        private readonly float[] _normValues = { 1.0F / 0.229F / 255.0F, 1.0F / 0.224F / 255.0F, 1.0F / 0.225F / 255.0F };

        private InferenceSession _dbNet;

        private List<string> _inputNames;

        internal DbNet() { }

        ~DbNet()
        {
            _dbNet.Dispose();
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
                    Console.WriteLine($"[DbNet] DirectML initialized on device");
                }
                catch (Exception dmlEx)
                {
                    // Fallback CPU, if DirectML now awailable
                    Console.WriteLine($"[DbNet] DirectML failed ({dmlEx.Message}), falling back to CPU.");

                    op.AppendExecutionProvider_CPU(0);

                    op.InterOpNumThreads = numThread;
                    op.IntraOpNumThreads = numThread;
                }

                _dbNet = new InferenceSession(path, op);
                _inputNames = _dbNet.InputMetadata.Keys.ToList();

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                throw;
            }
        }

        public List<TextBox> GetTextBoxes(Mat src, ScaleParam scale, float boxScoreThresh, float boxThresh, float unClipRatio)
        {
            Mat srcResize = new Mat();
            Cv2.Resize(src, srcResize, new Size(scale.DstWidth, scale.DstHeight));

            Tensor<float> inputTensors = OcrUtils.SubstractMeanNormalize(srcResize, _meanValues, _normValues);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputNames[0], inputTensors)
            };
            try
            {
                using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _dbNet.Run(inputs))
                {
                    var resultsArray = results.ToArray();
                    Console.WriteLine(resultsArray);

                    var textBoxes = GetTextBoxes(resultsArray, srcResize.Rows, srcResize.Cols, scale, boxScoreThresh, boxThresh, unClipRatio);

                    return textBoxes;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                //throw;
            }
            return null;
        }

        private static List<TextBox> GetTextBoxes(DisposableNamedOnnxValue[] outputTensor, int rows, int cols, ScaleParam s, float boxScoreThresh, float boxThresh, float unClipRatio)
        {
            float maxSideThresh = 3.0f;
            List<TextBox> rsBoxes = new List<TextBox>();

            //-----Data preparation-----
            float[] predData = outputTensor[0].AsEnumerable<float>().ToArray();
            List<byte> cbufData = new List<byte>();
            foreach (float data in predData)
            {
                var val = data * 255;
                cbufData.Add(Convert.ToByte(val));
            }

            Mat predMat = Mat.FromPixelData(rows, cols, MatType.CV_32FC1, predData);
            Mat cbufMat = Mat.FromPixelData(rows, cols, MatType.CV_8UC1, cbufData.ToArray());

            //-----boxThresh-----
            Mat thresholdMat = new Mat();

            Cv2.Threshold(cbufMat, thresholdMat, boxThresh * 255.0, 255.0, ThresholdTypes.Binary);

            //-----dilate-----
            Mat dilateMat = new Mat();

            Mat dilateElement = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2), new Point(-1, -1));

            Cv2.Dilate(thresholdMat, dilateMat, dilateElement, new Point(-1, -1), 1, BorderTypes.Default, Scalar.Gray);

            Cv2.FindContours(dilateMat, out Point[][] contours, out HierarchyIndex[] hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxSimple);

            for (int i = 0; i < contours.Length; i++)
            {
                if (contours[i].Length <= 2)
                {
                    continue;
                }

                float maxSide = 0;
                List<Point2f> minBox = GetMiniBox(contours[i], out maxSide);
                if (maxSide < maxSideThresh)
                {
                    continue;
                }

                double score = GetScore(contours[i], predMat);
                if (score < boxScoreThresh)
                {
                    continue;
                }

                List<Point> clipBox = Unclip(minBox, unClipRatio);
                if (clipBox == null)
                {
                    continue;
                }

                List<Point2f> clipMinBox = GetMiniBox(clipBox, out maxSide);
                if (maxSide < maxSideThresh + 2)
                {
                    continue;
                }

                List<Point> finalPoints = new List<Point>();
                foreach (var item in clipMinBox)
                {
                    int x = (int)(item.X / s.ScaleWidth);
                    int ptx = Math.Min(Math.Max(x, 0), s.SrcWidth);

                    int y = (int)(item.Y / s.ScaleHeight);
                    int pty = Math.Min(Math.Max(y, 0), s.SrcHeight);
                    Point dstPt = new Point(ptx, pty);
                    finalPoints.Add(dstPt);
                }

                TextBox textBox = new TextBox();
                textBox.Score = (float)score;
                textBox.Points = finalPoints;
                rsBoxes.Add(textBox);
            }
            rsBoxes.Reverse();
            return rsBoxes;
        }
        private static List<Point2f> GetMiniBox(List<Point> contours, out float minEdgeSize)
        {
            return GetMiniBox(contours.ToArray(), out minEdgeSize);
        }

        private static List<Point2f> GetMiniBox(Point[] contours, out float minEdgeSize)
        {
            List<Point2f> box = new List<Point2f>();

            RotatedRect rect = Cv2.MinAreaRect(contours);

            Point2f[] points = Cv2.BoxPoints(rect);
            minEdgeSize = Math.Min(rect.Size.Width, rect.Size.Height);

            List<Point2f> thePoints = new List<Point2f>(points);
            thePoints.Sort(CompareByX);

            int index_1 = 0, index_2 = 1, index_3 = 2, index_4 = 3;
            if (thePoints[1].Y > thePoints[0].Y)
            {
                index_1 = 0;
                index_4 = 1;
            }
            else
            {
                index_1 = 1;
                index_4 = 0;
            }

            if (thePoints[3].Y > thePoints[2].Y)
            {
                index_2 = 2;
                index_3 = 3;
            }
            else
            {
                index_2 = 3;
                index_3 = 2;
            }

            box.Add(thePoints[index_1]);
            box.Add(thePoints[index_2]);
            box.Add(thePoints[index_3]);
            box.Add(thePoints[index_4]);

            return box;
        }

        public static int CompareByX(Point2f left, Point2f right)
        {
            if (left == default && right == default)
            {
                return 1;
            }

            if (left == default)
            {
                return 0;
            }

            if (right == default)
            {
                return 1;
            }

            if (left.X > right.X)
            {
                return 1;
            }

            if (left.X == right.X)
            {
                return 0;
            }

            return -1;
        }

        private static double GetScore(Point[] contour, Mat fMapMat)
        {
            short xMin = 9999;
            short xMax = 0;
            short yMin = 9999;
            short yMax = 0;

            try
            {
                foreach (Point point in contour)
                {
                    if (point.X < xMin) xMin = (short)point.X;
                    if (point.X > xMax) xMax = (short)point.X;
                    if (point.Y < yMin) yMin = (short)point.Y;
                    if (point.Y > yMax) yMax = (short)point.Y;
                }

                int roiWidth = xMax - xMin + 1;
                int roiHeight = yMax - yMin + 1;

                using (Mat roiBitmap = new Mat(fMapMat, new Rect(xMin, yMin, roiWidth, roiHeight)))
                {
                    using (Mat mask = Mat.Zeros(roiHeight, roiWidth, MatType.CV_8UC1))
                    {
                        Point[][] shiftedContours = new Point[][]
                        {
                            contour.Select(p => new Point(p.X - xMin, p.Y - yMin)).ToArray()
                        };

                        Cv2.FillPoly(mask, shiftedContours, new Scalar(1));

                        return Cv2.Mean(roiBitmap, mask).Val0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }

            return 0;
        }

        private static List<Point> Unclip(List<Point2f> box, float unclip_ratio)
        {
            RotatedRect clipRect = Cv2.MinAreaRect(box);
            if (clipRect.Size.Height < 1.001 && clipRect.Size.Width < 1.001)
            {
                return null;
            }

            Path64 theCliperPts = new Path64();
            foreach (var pt in box)
            {
                theCliperPts.Add(new Point64((int)pt.X, (int)pt.Y));
            }

            float area = Math.Abs(SignedPolygonArea(box.ToArray<Point2f>()));
            double length = LengthOfPoints(box);
            double distance = area * unclip_ratio / length;

            ClipperOffset co = new ClipperOffset();
            co.AddPath(theCliperPts, JoinType.Round, EndType.Polygon);

            Paths64 solution = new Paths64();

            co.Execute(distance, solution);
            if (solution.Count == 0)
            {
                return null;
            }

            List<Point> retPts = new List<Point>();
            foreach (Point64 ip in solution[0])
            {
                retPts.Add(new Point((int)ip.X, (int)ip.Y));
            }

            return retPts;
        }

        private static float SignedPolygonArea(Point2f[] Points)
        {
            // Add the first point to the end.
            int num_points = Points.Length;
            Point2f[] pts = new Point2f[num_points + 1];
            Points.CopyTo(pts, 0);
            pts[num_points] = Points[0];

            // Get the areas.
            float area = 0;
            for (int i = 0; i < num_points; i++)
            {
                area +=
                    (pts[i + 1].X - pts[i].X) *
                    (pts[i + 1].Y + pts[i].Y) / 2;
            }

            return area;
        }

        private static double LengthOfPoints(List<Point2f> box)
        {
            double length = 0;

            Point2f pt = box[0];
            double x0 = pt.X;
            double y0 = pt.Y;
            double x1 = 0, y1 = 0, dx = 0, dy = 0;
            box.Add(pt);

            int count = box.Count;
            for (int idx = 1; idx < count; idx++)
            {
                Point2f pts = box[idx];
                x1 = pts.X;
                y1 = pts.Y;
                dx = x1 - x0;
                dy = y1 - y0;

                length += Math.Sqrt(dx * dx + dy * dy);

                x0 = x1;
                y0 = y1;
            }

            box.RemoveAt(count - 1);
            return length;
        }

        public void Dispose()
        {
            _dbNet.Dispose();
        }
    }
}
