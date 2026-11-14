using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace DocumentRecognition.OnnxRecognizer
{
    /// <summary>
    /// Единая точка создания <see cref="InferenceSession"/> для всех inference-движков:
    /// настройка оптимизации графа, выбор провайдера выполнения (CPU / DirectML / Auto),
    /// проба работоспособности DirectML и понятная ошибка, если файл модели отсутствует.
    /// </summary>
    internal static class OnnxSessionFactory
    {
        public static InferenceSession Create(
            string modelPath,
            string ownerName,
            InferenceExecutionProvider provider = InferenceExecutionProvider.Auto,
            int threads = 0,
            GraphOptimizationLevel optimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
                throw new ArgumentException("Не задан путь к ONNX-модели для " + ownerName + ".", nameof(modelPath));

            if (!File.Exists(modelPath))
                throw new FileNotFoundException("Файл модели для " + ownerName + " не найден: " + modelPath, modelPath);

            switch (provider)
            {
                case InferenceExecutionProvider.Cpu:
                    return CreateOnCpu(modelPath, ownerName, threads, optimizationLevel);

                case InferenceExecutionProvider.DirectMl:
                    return CreateOnDirectMl(modelPath, ownerName, threads, optimizationLevel);

                default:
                    return CreateAuto(modelPath, ownerName, threads, optimizationLevel);
            }
        }

        /// <summary>
        /// Auto: DirectML с проверкой пробным инференсом, при неудаче — CPU.
        /// </summary>
        /// <remarks>
        /// Проверка нужна потому, что DirectML умеет падать не только при создании сессии,
        /// но и во время Run (некорректная реализация отдельных операторов).
        /// В таком случае ORT не переключается на CPU автоматически.
        /// </remarks>
        private static InferenceSession CreateAuto(
            string modelPath,
            string ownerName,
            int threads,
            GraphOptimizationLevel optimizationLevel)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine($"[{ownerName}] DirectML недоступен на этой ОС, используется CPU.");
                return CreateOnCpu(modelPath, ownerName, threads, optimizationLevel);
            }

            InferenceSession dmlSession = null;

            try
            {
                dmlSession = CreateOnDirectMl(modelPath, ownerName, threads, optimizationLevel);

                if (TryProbe(dmlSession, ownerName))
                    return dmlSession;

                dmlSession.Dispose();
                dmlSession = null;

                Console.WriteLine($"[{ownerName}] DirectML не прошёл проверку, переход на CPU.");
            }
            catch (Exception ex)
            {
                dmlSession?.Dispose();
                Console.WriteLine($"[{ownerName}] DirectML failed ({ex.Message}), falling back to CPU.");
            }

            return CreateOnCpu(modelPath, ownerName, threads, optimizationLevel);
        }

        private static InferenceSession CreateOnCpu(
            string modelPath,
            string ownerName,
            int threads,
            GraphOptimizationLevel optimizationLevel)
        {
            using (var options = CreateBaseOptions(optimizationLevel, threads))
            {
                options.AppendExecutionProvider_CPU();
                var session = new InferenceSession(modelPath, options);
                Console.WriteLine($"[{ownerName}] CPU initialized");
                return session;
            }
        }

        private static InferenceSession CreateOnDirectMl(
            string modelPath,
            string ownerName,
            int threads,
            GraphOptimizationLevel optimizationLevel)
        {
            using (var options = CreateBaseOptions(optimizationLevel, threads))
            {
                options.AppendExecutionProvider_DML();
                var session = new InferenceSession(modelPath, options);
                Console.WriteLine($"[{ownerName}] DirectML initialized");
                return session;
            }
        }

        private static SessionOptions CreateBaseOptions(GraphOptimizationLevel optimizationLevel, int threads)
        {
            var options = new SessionOptions
            {
                GraphOptimizationLevel = optimizationLevel
            };

            if (threads > 0)
            {
                options.IntraOpNumThreads = threads;
                options.InterOpNumThreads = threads;
            }

            return options;
        }

        /// <summary>
        /// Пробный инференс на тензорах нулей, собранных по метаданным входов модели.
        /// Возвращает <c>true</c>, если проверку выполнить не удалось (неизвестная форма входа)
        /// или она прошла успешно.
        /// </summary>
        private static bool TryProbe(InferenceSession session, string ownerName)
        {
            try
            {
                var inputs = new List<NamedOnnxValue>();

                foreach (var input in session.InputMetadata)
                {
                    var dimensions = input.Value.Dimensions;

                    if (dimensions.Length == 4)
                    {
                        var height = dimensions[2] > 0 ? dimensions[2] : 640;
                        var width = dimensions[3] > 0 ? dimensions[3] : 640;

                        inputs.Add(NamedOnnxValue.CreateFromTensor(
                            input.Key,
                            new DenseTensor<float>(new float[3 * height * width], new[] { 1, 3, height, width })));
                    }
                    else if (dimensions.Length == 2)
                    {
                        // im_shape и scale_factor: модели Paddle ждут осмысленные значения.
                        var isShape = input.Key.IndexOf("shape", StringComparison.OrdinalIgnoreCase) >= 0;
                        var values = isShape
                            ? new[] { 640f, 640f }
                            : new[] { 0.14f, 0.14f };

                        inputs.Add(NamedOnnxValue.CreateFromTensor(
                            input.Key,
                            new DenseTensor<float>(values, new[] { 1, 2 })));
                    }
                    else
                    {
                        // Форму входа не знаем — проверку пропускаем.
                        return true;
                    }
                }

                using (session.Run(inputs))
                {
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{ownerName}] probe failed: {ex.Message}");
                return false;
            }
        }
    }
}
