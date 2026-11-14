namespace DocumentRecognition.WebApi.Models
{
    /// <summary>
    /// Параметры запроса распознавания чертежа (multipart/form-data).
    /// </summary>
    public sealed class RecognizeDrawingRequest
    {
        /// <summary>
        /// PDF-файл с чертежом (поле формы <c>file</c>).
        /// </summary>
        public IFormFile? File { get; set; }

        /// <summary>
        /// DPI растеризации PDF. Обычно 200–900, по умолчанию 200.
        /// </summary>
        public int? Dpi { get; set; }

        /// <summary>
        /// Не рисовать отладочные рамки детекций на изображении результата.
        /// По умолчанию <c>true</c>.
        /// </summary>
        public bool? CleanImage { get; set; }
    }
}
