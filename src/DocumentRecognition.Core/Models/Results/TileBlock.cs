using DocumentRecognition.Core.Enums;
using System;
using System.Collections.Generic;

namespace DocumentRecognition.Core.Models.Results
{
    public class TitleBlock
    {
        public string Name { get; set; }
        public string Designation { get; set; }
        public int DocumentType { get; set; }
        public List<Signature> Signatures { get; set; }
        public int SheetNumber { get; set; }
        public int TotalSheets { get; set; }
        public string Mass { get; set; }
        public string Scale { get; set; }

        public TitleBlock() { }
    }

    public class Signature
    {
        public Signatories Signatory { get; private set; }
        public string Name { get; private set; }
        public DateTime Date { get; private set; }

        public Signature(Signatories signatory, DateTime date, string name)
        {
            Signatory = signatory;
            Date = date;
            Name = name;
        }
    }
}
