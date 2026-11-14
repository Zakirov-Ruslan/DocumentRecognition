namespace DocumentRecognition.Core.Models.Options
{
    /// <summary>
    /// Параметры обработки документа для одного запроса распознавания.
    /// </summary>
    /// <remarks>
    /// Отделено от <c>OcrSettings</c> (Infrastructure): там настройки движков и пути к моделям,
    /// которые задаются при старте приложения, здесь — переменные параметры конкретного запроса
    /// (консоль, HTTP-запрос, UI). Модель живёт в Core, чтобы её могли использовать все слои.
    /// </remarks>
    public sealed class DrawingRecognitionOptions
    {
        /// <summary>
        /// DPI растеризации PDF по умолчанию.
        /// </summary>
        public const int DefaultDpi = 200;

        /// <summary>
        /// Минимальный допустимый DPI: ниже качество OCR мелкого текста деградирует.
        /// </summary>
        public const int MinDpi = 200;

        /// <summary>
        /// Максимальный допустимый DPI: выше резко растут время распознавания и потребление памяти.
        /// </summary>
        public const int MaxDpi = 900;

        /// <summary>
        /// DPI растеризации PDF. Как правило используется диапазон 200–900, по умолчанию 200.
        /// Влияет на точность OCR и на пересчёт пикселей в миллиметры в UI.
        /// </summary>
        public int Dpi { get; set; } = DefaultDpi;

        /// <summary>
        /// <c>true</c> (по умолчанию) — возвращать изображение результата без отрисованных
        /// отладочных рамок детекций. Координаты блоков при этом доступны в
        /// <see cref="Results.DrawingRecognitionResult.Regions"/>.
        /// </summary>
        public bool CleanImage { get; set; } = true;

        /// <summary>
        /// Проверяет, попадает ли значение DPI в допустимый диапазон.
        /// </summary>
        public static bool IsDpiInRange(int dpi)
        {
            return dpi >= MinDpi && dpi <= MaxDpi;
        }

        /// <summary>
        /// Приводит значения к допустимым: DPI вне диапазона зажимается в [<see cref="MinDpi"/>, <see cref="MaxDpi"/>].
        /// </summary>
        /// <remarks>
        /// Вызывается в начале обработки, чтобы все точки входа (консоль, HTTP, UI)
        /// не дублировали проверки и всегда получали предсказуемый результат.
        /// </remarks>
        public void Normalize()
        {
            if (Dpi < MinDpi)
                Dpi = MinDpi;
            else if (Dpi > MaxDpi)
                Dpi = MaxDpi;
        }
    }
}
