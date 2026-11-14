/*
 * Modified by Zakirov-Ruslan
 * Original: https://github.com/RapidAI/RapidOCRCSharp
 */

using System.Collections.Generic;
using System.Text;

namespace Ocr.Models
{
    public sealed class TextLine
    {
        public string Text { get; set; }
        public List<float> CharScores { get; set; } = new List<float>();
        public float Time { get; set; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            CharScores.ForEach(x => sb.Append($"{x},"));
            return $"TextLine[Text({Text}),CharScores({sb.ToString()}),Time({Time}ms)]";
        }
    }
}
