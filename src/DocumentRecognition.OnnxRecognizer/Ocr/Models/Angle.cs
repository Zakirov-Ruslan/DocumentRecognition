/*
 * Modified by Zakirov-Ruslan
 * Original: https://github.com/RapidAI/RapidOCRCSharp
 */

namespace Ocr.Models
{
    public sealed class Angle
    {
        public int Index { get; set; }
        public float Score { get; set; }
        public float Time { get; set; }
        public override string ToString()
        {
            string header = Index >= 0 ? "Angle" : "AngleDisabled";
            return $"{header}[Index({Index}), Score({Score}), Time({Time}ms)]";
        }
    }
}
