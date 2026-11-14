namespace DocumentRecognition.OnnxRecognizer
{
    /// <summary>
    /// Провайдер выполнения ONNX-инференса.
    /// </summary>
    public enum InferenceExecutionProvider
    {
        /// <summary>
        /// DirectML, если он доступен и работоспособен, иначе CPU.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Только CPU.
        /// </summary>
        Cpu = 1,

        /// <summary>
        /// Только DirectML (GPU). Если провайдер недоступен — ошибка без отката на CPU.
        /// </summary>
        DirectMl = 2
    }
}
