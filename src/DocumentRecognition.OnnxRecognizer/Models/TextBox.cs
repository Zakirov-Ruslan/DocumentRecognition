/*
 * Modified by Zakirov-Ruslan
 * Original: https://github.com/RapidAI/RapidOCRCSharp
 */

using OpenCvSharp;
using System.Collections.Generic;

namespace Ocr.Models
{
    public sealed class TextBox
    {
        public List<Point> Points { get; set; }
        public float Score { get; set; }
        public override string ToString()
        {
            return $"TextBox[score({Score}),[x: {Points[0].X}, y: {Points[0].Y}], [x: {Points[1].X}, y: {Points[1].Y}], [x: {Points[2].X}, y: {Points[2].Y}], [x: {Points[3].X}, y: {Points[3].Y}]]";
        }
    }
}
