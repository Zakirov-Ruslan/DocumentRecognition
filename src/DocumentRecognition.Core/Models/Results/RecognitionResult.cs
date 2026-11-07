namespace DocumentRecognition.Core.Models.Results
{
    /// <summary>
    /// Базовый тип результата распознавания документа.
    /// </summary>
    /// <remarks>
    /// Задел на будущее: когда появятся другие виды документов, их результаты
    /// (например, SpecificationRecognitionResult) будут наследоваться от этого типа,
    /// чтобы их можно было обрабатывать единообразно.
    /// </remarks>
    public abstract class RecognitionResult
    {
    }
}