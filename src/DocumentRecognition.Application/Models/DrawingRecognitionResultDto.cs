using DocumentRecognition.Core.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DocumentRecognition.Application.Models
{
    /// <summary>
    /// Сериализуемое представление результата распознавания чертежа.
    /// Используется API и консолью, чтобы наружу не уходили служебные поля модели.
    /// </summary>
    public class DrawingRecognitionResultDto
    {
        public TitleBlockDto TitleBlock { get; set; }

        public IReadOnlyList<string> TechnicalRequirements { get; set; }

        /// <summary>
        /// Отладочное изображение в base64. Заполняется только при явном запросе.
        /// </summary>
        public string DebugImageBase64 { get; set; }

        /// <summary>
        /// Ширина обработанного изображения в пикселях (0, если неизвестна).
        /// </summary>
        public int ImageWidth { get; set; }

        /// <summary>
        /// Высота обработанного изображения в пикселях (0, если неизвестна).
        /// </summary>
        public int ImageHeight { get; set; }

        /// <summary>
        /// DPI растеризации PDF (нужен для перевода пикселей в миллиметры).
        /// </summary>
        public int Dpi { get; set; }

        /// <summary>
        /// Области результата распознавания с координатами. Заполняется только при явном запросе.
        /// </summary>
        public IReadOnlyList<RecognitionRegionDto> Regions { get; set; }

        public static DrawingRecognitionResultDto FromResult(DrawingRecognitionResult result, bool includeDebugImage = false, bool includeRegions = false)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            return new DrawingRecognitionResultDto
            {
                TitleBlock = TitleBlockDto.FromTitleBlock(result.TitleBlock),
                TechnicalRequirements = result.TechnicalRequirements ?? Array.Empty<string>(),
                DebugImageBase64 = includeDebugImage && result.DebugImage != null
                    ? Convert.ToBase64String(result.DebugImage)
                    : null,
                ImageWidth = result.ImageWidth,
                ImageHeight = result.ImageHeight,
                Dpi = result.Dpi,
                Regions = includeRegions && result.Regions != null
                    ? result.Regions.Select(RecognitionRegionDto.FromRegion).ToList()
                    : null
            };
        }
    }

    /// <summary>
    /// Область результата распознавания с координатами в пикселях обработанного изображения.
    /// </summary>
    public class RecognitionRegionDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>
        /// Тип области: <c>LayoutBlock</c>, <c>TitleBlockCell</c>, <c>OcrLine</c>, <c>TableCell</c>.
        /// </summary>
        public string Kind { get; set; }

        public string Label { get; set; }
        public string Key { get; set; }
        public string Text { get; set; }
        public float Score { get; set; }

        public static RecognitionRegionDto FromRegion(RecognitionRegion region)
        {
            if (region == null)
                return null;

            return new RecognitionRegionDto
            {
                X = region.Box.X,
                Y = region.Box.Y,
                Width = region.Box.Width,
                Height = region.Box.Height,
                Kind = region.Kind.ToString(),
                Label = region.Label,
                Key = region.Key,
                Text = region.Text,
                Score = region.Score
            };
        }
    }

    public class TitleBlockDto
    {
        public string Designation { get; set; }
        public string Name { get; set; }
        public string Mass { get; set; }
        public string Scale { get; set; }
        public string Material { get; set; }
        public string SheetNumber { get; set; }
        public string TotalSheets { get; set; }
        public IReadOnlyList<SignatureDto> Signatures { get; set; }

        public static TitleBlockDto FromTitleBlock(TitleBlock titleBlock)
        {
            if (titleBlock == null)
                return null;

            return new TitleBlockDto
            {
                Designation = titleBlock.Designation,
                Name = titleBlock.Name,
                Mass = titleBlock.Mass,
                Scale = titleBlock.Scale,
                Material = titleBlock.Material,
                SheetNumber = titleBlock.SheetNumber,
                TotalSheets = titleBlock.TotalSheets,
                Signatures = titleBlock.Signatures == null
                    ? new List<SignatureDto>()
                    : titleBlock.Signatures.Select(SignatureDto.FromSignature).ToList()
            };
        }
    }

    public class SignatureDto
    {
        /// <summary>
        /// Идентификатор должности (значение enum <c>Signatories</c>), например <c>DesignedBy</c>.
        /// </summary>
        public string Signatory { get; set; }

        /// <summary>
        /// Распознанный текст должности, например «Разраб.».
        /// </summary>
        public string SignatoryText { get; set; }

        public string Name { get; set; }
        public DateTime? Date { get; set; }

        public static SignatureDto FromSignature(Signature signature)
        {
            if (signature == null)
                return null;

            return new SignatureDto
            {
                Signatory = signature.Signatory.ToString(),
                SignatoryText = signature.SignatoryText,
                Name = signature.Name,
                Date = signature.Date
            };
        }
    }
}
