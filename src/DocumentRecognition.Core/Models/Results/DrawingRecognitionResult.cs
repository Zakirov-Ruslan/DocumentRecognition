namespace DocumentRecognition.Core.Models.Results
{
    public class DrawingRecognitionResult : RecognitionResult
    {
        public TitleBlock TitleBlock { get; private set; }
        public string[] TechnicalRequirements { get; private set; }
        public byte[] DebugImage { get; private set; }

        public DrawingRecognitionResult(TitleBlock titleBlock, string[] technicalRequirements, byte[] debugImage = null)
        {
            TitleBlock = titleBlock;
            TechnicalRequirements = technicalRequirements;
            DebugImage = debugImage;
        }
    }
}
