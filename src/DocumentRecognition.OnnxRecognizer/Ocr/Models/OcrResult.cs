/*
 * Modified by Zakirov-Ruslan
 * Original: https://github.com/RapidAI/RapidOCRCSharp
 */

using OpenCvSharp;
using System.Collections.Generic;
using System.Text;

namespace Ocr.Models
{
    public sealed class OcrResult
    {
        public List<TextBlock> TextBlocks { get; set; } = new List<TextBlock>();
        public float DbNetTime { get; set; }
        public Mat BoxImg { get; set; }
        public float DetectTime { get; set; }
        public string StrRes { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("OcrResult");

            TextBlocks.ForEach(x => {
                sb.Append(x);
            });

            sb.AppendLine($"├─DbNetTime({DbNetTime}ms)");
            sb.AppendLine($"├─DetectTime({DetectTime}ms)");
            sb.AppendLine($"└─StrRes({StrRes})");
            return sb.ToString();
        }
    }
}
