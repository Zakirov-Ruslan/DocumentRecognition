using DocumentRecognition.Application.Models;
using System.Text;

namespace DocumentRecognition.Console
{
    /// <summary>
    /// Человекочитаемое представление результата для консоли.
    /// </summary>
    internal static class DrawingResultFormatter
    {
        private const int LabelWidth = 16;
        private const string EmptyValue = "—";

        public static string ToText(string filePath, DrawingRecognitionResultDto result)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Файл: {filePath}");

            AppendTitleBlock(builder, result.TitleBlock);
            AppendTechnicalRequirements(builder, result.TechnicalRequirements);

            return builder.ToString().TrimEnd();
        }

        private static void AppendTitleBlock(StringBuilder builder, TitleBlockDto? titleBlock)
        {
            builder.AppendLine();

            if (titleBlock is null)
            {
                builder.AppendLine("Основная надпись не распознана.");
                return;
            }

            builder.AppendLine("Основная надпись");
            AppendField(builder, "Обозначение", titleBlock.Designation);
            AppendField(builder, "Наименование", titleBlock.Name);
            AppendField(builder, "Масса", titleBlock.Mass);
            AppendField(builder, "Масштаб", titleBlock.Scale);
            AppendField(builder, "Материал", titleBlock.Material);
            AppendField(builder, "Лист", titleBlock.SheetNumber);
            AppendField(builder, "Листов", titleBlock.TotalSheets);

            builder.AppendLine();
            builder.AppendLine("Подписи");

            var signatures = titleBlock.Signatures;
            if (signatures is null || signatures.Count == 0)
            {
                builder.AppendLine("  не найдены");
                return;
            }

            var rows = signatures
                .Select(signature => new
                {
                    Role = signature?.SignatoryText ?? string.Empty,
                    Name = signature?.Name ?? string.Empty,
                    Date = signature?.Date?.ToString("dd.MM.yyyy") ?? string.Empty
                })
                .ToList();

            var roleWidth = Math.Max(12, rows.Max(row => row.Role.Length));
            var nameWidth = Math.Max(20, rows.Max(row => row.Name.Length));

            foreach (var row in rows)
            {
                builder.AppendLine($"  {row.Role.PadRight(roleWidth)}  {row.Name.PadRight(nameWidth)}  {row.Date}");
            }
        }

        private static void AppendTechnicalRequirements(StringBuilder builder, IReadOnlyList<string>? requirements)
        {
            builder.AppendLine();
            builder.AppendLine("Технические требования");

            if (requirements is null || requirements.Count == 0)
            {
                builder.AppendLine("  не найдены");
                return;
            }

            for (var i = 0; i < requirements.Count; i++)
            {
                builder.AppendLine($"  {i + 1}. {requirements[i]}");
            }
        }

        private static void AppendField(StringBuilder builder, string label, string? value)
        {
            builder.Append("  ");
            builder.Append(label.PadRight(LabelWidth));
            builder.Append(": ");
            builder.AppendLine(string.IsNullOrWhiteSpace(value) ? EmptyValue : value);
        }
    }
}
