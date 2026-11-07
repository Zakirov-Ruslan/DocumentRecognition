using System.Collections.Generic;

namespace DocumentRecognition.Core.Models.Results
{
    public class TitleBlock
    {
        public string Name { get; set; }
        public string Designation { get; set; }
        public List<Signature> Signatures { get; set; }
        public string SheetNumber { get; set; }
        public string TotalSheets { get; set; }
        public string Mass { get; set; }
        public string Scale { get; set; }
        public string Material { get; set; }

        public TitleBlock() { }
    }
}
