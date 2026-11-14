using Microsoft.ML.OnnxRuntime;
using System;
using System.IO;

namespace DocumentRecognition.OnnxRecognizer
{
    /// <summary>
    /// Единая точка создания <see cref="InferenceSession"/> для всех inference-движков:
    /// настройка оптимизации графа, попытка включить DirectML с откатом на CPU
    /// и понятная ошибка, если файл модели отсутствует.
    /// </summary>
    internal static class OnnxSessionFactory
    {
        public static InferenceSession Create(
            string modelPath,
            string ownerName,
            GraphOptimizationLevel optimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
                throw new ArgumentException("Не задан путь к ONNX-модели для " + ownerName + ".", nameof(modelPath));

            if (!File.Exists(modelPath))
                throw new FileNotFoundException("Файл модели для " + ownerName + " не найден: " + modelPath, modelPath);

            var op = new SessionOptions();
            op.GraphOptimizationLevel = optimizationLevel;

            try
            {
                op.AppendExecutionProvider_DML();
                Console.WriteLine($"[{ownerName}] DirectML initialized");
            }
            catch (Exception dmlEx)
            {
                // Fallback CPU, if DirectML not available
                Console.WriteLine($"[{ownerName}] DirectML failed ({dmlEx.Message}), falling back to CPU.");

                op.AppendExecutionProvider_CPU(0);
            }

            return new InferenceSession(modelPath, op);
        }
    }
}
