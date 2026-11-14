/*
 * Modified by Zakirov-Ruslan
 * Original: https://github.com/RapidAI/RapidOCRCSharp
 */

using OpenCvSharp;
using System.Collections.Generic;
using System.Text;

namespace Ocr.Models
{
    public sealed class TextBlock
    {
        public List<Point> BoxPoints { get; set; }
        public float BoxScore { get; set; }
        public int AngleIndex { get; set; }
        public float AngleScore { get; set; }
        public float AngleTime { get; set; }
        public string Text { get; set; }
        public List<float> CharScores { get; set; } = new List<float>();
        public float CrnnTime { get; set; }
        public float BlockTime { get; set; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("├─TextBlock");
            string textBox = $"│   ├──TextBox[score({BoxScore}),[x: {BoxPoints[0].X}, y: {BoxPoints[0].Y}], [x: {BoxPoints[1].X}, y: {BoxPoints[1].Y}], [x: {BoxPoints[2].X}, y: {BoxPoints[2].Y}], [x: {BoxPoints[3].X}, y: {BoxPoints[3].Y}]]";
            sb.AppendLine(textBox);
            string header = AngleIndex >= 0 ? "Angle" : "AngleDisabled";
            string angle = $"│   ├──{header}[Index({AngleIndex}), Score({AngleScore}), Time({AngleTime}ms)]";
            sb.AppendLine(angle);
            StringBuilder sbScores = new StringBuilder();

            if (CharScores != null)
            {
                CharScores.ForEach(x => sbScores.Append($"{x},"));
                string textLine = $"│   ├──TextLine[Text({Text}),CharScores({sbScores.ToString()}),Time({CrnnTime}ms)]";
                sb.AppendLine(textLine);
                sb.AppendLine($"│   └──BlockTime({BlockTime}ms)");
            }

            return sb.ToString();
        }
    }
}
