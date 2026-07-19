using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System;
using System.Linq;

public class DocOrientationClassifier : IDisposable
{
    private InferenceSession _session;
    private readonly int _inputSize = 224;

    private readonly float[] _mean = { 0.485f, 0.456f, 0.406f };
    private readonly float[] _std = { 0.229f, 0.224f, 0.225f };

    public DocOrientationClassifier(string onnxPath) 
    {
        var opts = new SessionOptions();
        opts.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        _session = new InferenceSession(onnxPath, opts);
    }

    public Mat EnsureImageOrientation(Mat img)
    {
        if (img.Empty())
            throw new ArgumentException("Input image is empty", nameof(img));

        int rotation = ClassifyOrientation(img);

        if (rotation == 0)
            return img.Clone();

        Mat rotated = new Mat();

        switch (rotation)
        {
            case 90:
                Cv2.Rotate(img, rotated, RotateFlags.Rotate90Counterclockwise);
                break;
            case 180:
                Cv2.Rotate(img, rotated, RotateFlags.Rotate180);
                break;
            case 270:
                Cv2.Rotate(img, rotated, RotateFlags.Rotate90Clockwise);
                break;
            default:
                rotated = img;
                break;
        }

        return rotated;
    }

    public int ClassifyOrientation(string imagePath)
    {
        using (var img = Cv2.ImRead(imagePath, ImreadModes.Color))
        {
            if (img.Empty())
                throw new ArgumentException("Input image is empty", nameof(img));

            return ClassifyOrientation(img);
        }
    }

    public int ClassifyOrientation(Mat img)
    {
        if (img.Empty())
            throw new ArgumentException("Input image is empty", nameof(img));

        using (var resized = new Mat())
        {
            Cv2.Resize(img, resized, new Size(_inputSize, _inputSize));

            using (var rgb = new Mat())
            {
                Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);

                var inputTensor = new DenseTensor<float>(new[] { 1, 3, _inputSize, _inputSize });

                byte[] rawData = new byte[_inputSize * _inputSize * 3];
                System.Runtime.InteropServices.Marshal.Copy(rgb.Data, rawData, 0, rawData.Length);

                for (int y = 0; y < _inputSize; y++)
                {
                    for (int x = 0; x < _inputSize; x++)
                    {
                        int idx = (y * _inputSize + x) * 3;

                        byte r = rawData[idx];
                        byte g = rawData[idx + 1];
                        byte b = rawData[idx + 2];

                        inputTensor[0, 0, y, x] = (r / 255.0f - _mean[0]) / _std[0];
                        inputTensor[0, 1, y, x] = (g / 255.0f - _mean[1]) / _std[1];
                        inputTensor[0, 2, y, x] = (b / 255.0f - _mean[2]) / _std[2];
                    }
                }

                // Inference
                string inputName = _session.InputMetadata.Keys.First(); 
                var inputs = new[] { NamedOnnxValue.CreateFromTensor(inputName, inputTensor) };

                using (var results = _session.Run(inputs))
                {
                    // Postprocess: softmax + argmax
                    var outputTensor = results.First().AsTensor<float>();
                    float[] logits = outputTensor.ToArray();
                    float[] probabilities = Softmax(logits);
                    int predictedClass = Array.IndexOf(probabilities, probabilities.Max());

                    int[] angles = { 0, 90, 180, 270 };
                    return angles[predictedClass];
                }
            }
        }
    }

    private float[] Softmax(float[] logits)
    {
        float maxLogit = logits.Max();
        float sumExp = logits.Sum(x => (float)Math.Exp(x - maxLogit));
        return logits.Select(x => (float)Math.Exp(x - maxLogit) / sumExp).ToArray();
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}