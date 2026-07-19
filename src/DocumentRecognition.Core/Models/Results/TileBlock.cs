using DocumentRecognition.Core.Enums;
using System;
using System.Collections.Generic;

namespace DocumentRecognition.Core.Models.Results
{
    public class TileBlock
    {
        public string Name { get; private set; }
        public string Designation { get; private set; }
        public int DocumentType { get; private set; }
        public List<Signature> Signatures { get; private set; }
        public int SheetNumber { get; private set; }
        public int TotalSheets { get; private set; }

        public TileBlock(string name, string designation, int documentType, List<Signature> signatures, int sheetNumber, int totalSheets)
        {
            Name = name;
            Designation = designation;
            DocumentType = documentType;
            Signatures = signatures;
            SheetNumber = sheetNumber;
            TotalSheets = totalSheets;
        }
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
