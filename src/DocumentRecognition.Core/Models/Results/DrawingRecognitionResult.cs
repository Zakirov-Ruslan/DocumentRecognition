using DocumentRecognition.Core.Models;
using System;
using System.Collections.Generic;

namespace DocumentRecognition.Core.Models.Results
{
    public class DrawingRecognitionResult : RecognitionResult
    {
        public TitleBlock TitleBlock { get; private set; }
        public string[] TechnicalRequirements { get; private set; }
        public byte[] DebugImage { get; private set; }

        /// <summary>
        /// Области результата распознавания (блоки разметки, ячейки основной надписи,
        /// OCR-строки, ячейки таблиц) с координатами в пикселях <see cref="ImageWidth"/>×<see cref="ImageHeight"/>.
        /// Используются для интерактивной подсветки и тултипов в UI.
        /// </summary>
        public IReadOnlyList<RecognitionRegion> Regions { get; private set; }

        /// <summary>
        /// Ширина обработанного изображения в пикселях (0, если неизвестна).
        /// </summary>
        public int ImageWidth { get; private set; }

        /// <summary>
        /// Высота обработанного изображения в пикселях (0, если неизвестна).
        /// </summary>
        public int ImageHeight { get; private set; }

        /// <summary>
        /// DPI, с которым PDF был растеризован. Позволяет переводить пиксели в миллиметры.
        /// </summary>
        public int Dpi { get; private set; }

        public DrawingRecognitionResult(
            TitleBlock titleBlock,
            string[] technicalRequirements,
            byte[] debugImage = null,
            IReadOnlyList<RecognitionRegion> regions = null,
            int imageWidth = 0,
            int imageHeight = 0,
            int dpi = 0)
        {
            TitleBlock = titleBlock;
            TechnicalRequirements = technicalRequirements;
            DebugImage = debugImage;
            Regions = regions ?? Array.Empty<RecognitionRegion>();
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            Dpi = dpi;
        }
    }
}
