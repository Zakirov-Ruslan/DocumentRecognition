using System.Threading.Tasks;

namespace DocumentRecognition.OnnxRecognizer
{
    /// <summary>
    /// Единый контракт ленивой инициализации inference-движков (ONNX-моделей).
    /// Конструктор движка должен быть дешёвым: он только сохраняет параметры,
    /// а тяжёлая загрузка модели выполняется один раз при первом инференсе
    /// или при явном вызове <see cref="InitializeAsync"/> (прогрев).
    /// </summary>
    public interface IInferenceEngine
    {
        /// <summary>true, если модель уже загружена в память.</summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Загружает модель. Вызов идемпотентен и потокобезопасен:
        /// повторные вызовы не пересоздают сессию.
        /// </summary>
        Task InitializeAsync();
    }
}
