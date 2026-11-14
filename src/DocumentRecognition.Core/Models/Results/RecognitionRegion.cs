using DocumentRecognition.Core.Enums;

namespace DocumentRecognition.Core.Models.Results
{
    /// <summary>
    /// Область результата распознавания с координатами и связанными данными.
    /// </summary>
    /// <remarks>
    /// Используется для интерактивного просмотра изображения: подсветка областей,
    /// тултипы с координатами и распознанным текстом, связь области со строкой
    /// в таблице результатов (<see cref="Key"/>).
    /// Координаты — в пикселях полного обработанного изображения.
    /// </remarks>
    public sealed class RecognitionRegion
    {
        public RecognitionRegion(
            PixelBox box,
            RecognitionRegionKind kind,
            string label = null,
            string key = null,
            string text = null,
            float score = 0f)
        {
            Box = box;
            Kind = kind;
            Label = label;
            Key = key;
            Text = text;
            Score = score;
        }

        /// <summary>
        /// Границы области в координатах изображения.
        /// </summary>
        public PixelBox Box { get; }

        /// <summary>
        /// Тип области (блок разметки, ячейка надписи, OCR-строка, ячейка таблицы).
        /// </summary>
        public RecognitionRegionKind Kind { get; }

        /// <summary>
        /// Человекочитаемая метка: класс блока (<c>title_block_2</c>),
        /// идентификатор ячейки надписи (<c>designation</c>) или имя таблицы.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Ключ для связи области с элементом UI (например, строкой таблицы результатов).
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Распознанный текст области, если он есть.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Уверенность детекции/распознавания (0..1), если известна.
        /// </summary>
        public float Score { get; }

        public override string ToString()
        {
            return $"{Kind} [{Label}] {Box}";
        }
    }
}
