namespace DocumentRecognition.Core.Models.Results
{
    public class DrawingRecognitionResult : RecognitionResult
    {
        public TileBlock TileBlock { get; private set; }
        public string[] TechnicalRequirements { get; private set; }
        public byte[] DebugImage { get; private set; }

        public DrawingRecognitionResult(TileBlock tileBlock, string[] technicalRequirements, byte[] debugImage = null)
        {
            TileBlock = tileBlock;
            TechnicalRequirements = technicalRequirements;
            DebugImage = debugImage;
        }
    }
}
