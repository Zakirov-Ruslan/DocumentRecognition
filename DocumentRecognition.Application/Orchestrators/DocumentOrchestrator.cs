using DocumentRecognition.Core.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace DocumentRecognition.Application.Orchestrators
{
    public class DocumentOrchestrator
    {
        private readonly ILayoutDetector _layoutDetector;
        private readonly ITextRecognizer _recognizer;

        public DocumentOrchestrator(ILayoutDetector layoutDetector, ITextRecognizer recognizer)
        {
            _layoutDetector = layoutDetector;
            _recognizer = recognizer;
        }

        public async Task ProcessAsync(Stream imageStream)
        {
            // 1. Layout detecting
            //var layout = await _layoutDetector.DetectAsync(imageStream);

            //// 3. Распознавание
            //var recognitionResult = await _recognizer.RecognizeAsync(imageStream, layout);

            //return;
        }
    }
}
