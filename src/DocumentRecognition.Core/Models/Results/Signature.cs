using DocumentRecognition.Core.Enums;
using System;

namespace DocumentRecognition.Core.Models.Results
{
    public class Signature
    {
        public Signatories Signatory { get; set; }
        public string SignatoryText { get; set; }
        public string Name { get; set; }
        public DateTime? Date { get; set; }

        public Signature() { }
    }
}
