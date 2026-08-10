using System;

namespace Ocr.Models
{
    /// <summary>
    /// Configuration options for OCR detection
    /// </summary>
    public class DetectOptions
    {
        /// <summary>
        /// Padding from image edges (recommended 10-50). 
        /// Improves text detection at borders.
        /// Default: 50
        /// </summary>
        public int Padding { get; set; } = 50;

        /// <summary>
        /// Maximum side length for image scaling.
        /// Default: 1024
        /// </summary>
        public int MaxSideLen { get; set; } = 1024;

        /// <summary>
        /// Confidence threshold for text boxes (0.0-1.0).
        /// Default: 0.5
        /// </summary>
        public float BoxScoreThresh { get; set; } = 0.5f;

        /// <summary>
        /// Reliability threshold for text areas (0.0-1.0).
        /// Default: 0.3
        /// </summary>
        public float BoxThresh { get; set; } = 0.3f;

        /// <summary>
        /// Expansion ratio for text boxes.
        /// Default: 1.6
        /// </summary>
        public float UnClipRatio { get; set; } = 1.6f;

        /// <summary>
        /// Enable text angle detection.
        /// Default: true
        /// </summary>
        public bool DoAngle { get; set; } = true;

        /// <summary>
        /// Support text recognition at any angle (requires DoAngle=true).
        /// Default: false
        /// </summary>
        public bool MostAngle { get; set; } = false;

        /// <summary>
        /// Split vertical text boxes into horizontal lines.
        /// Useful for numbered lists with long spaces between number and text.
        /// Default: false
        /// </summary>
        public bool OnlyHorizontalLines { get; set; } = false;

        /// <summary>
        /// Enable batching for AngleNet and CrnnNet (faster on GPU).
        /// Default: true
        /// </summary>
        public bool UseBatching { get; set; } = true;

        /// <summary>
        /// Batch size for inference (only used when UseBatching=true).
        /// Default: 16
        /// </summary>
        public int BatchSize { get; set; } = 16;

        /// <summary>
        /// Creates default options
        /// </summary>
        public DetectOptions() { }

        /// <summary>
        /// Creates options with custom values
        /// </summary>
        public DetectOptions(
            int padding = 50,
            int maxSideLen = 1024,
            float boxScoreThresh = 0.5f,
            float boxThresh = 0.3f,
            float unClipRatio = 1.6f,
            bool doAngle = true,
            bool mostAngle = false,
            bool onlyHorizontalLines = false,
            bool useBatching = true,
            int batchSize = 16)
        {
            Padding = padding;
            MaxSideLen = maxSideLen;
            BoxScoreThresh = boxScoreThresh;
            BoxThresh = boxThresh;
            UnClipRatio = unClipRatio;
            DoAngle = doAngle;
            MostAngle = mostAngle;
            OnlyHorizontalLines = onlyHorizontalLines;
            UseBatching = useBatching;
            BatchSize = batchSize;
        }

        /// <summary>
        /// Validates options and throws if invalid
        /// </summary>
        public void Validate()
        {
            if (Padding < 0)
                throw new ArgumentException("Padding must be >= 0");

            if (MaxSideLen <= 0)
                throw new ArgumentException("MaxSideLen must be > 0");

            if (BoxScoreThresh < 0 || BoxScoreThresh > 1)
                throw new ArgumentException("BoxScoreThresh must be between 0 and 1");

            if (BoxThresh < 0 || BoxThresh > 1)
                throw new ArgumentException("BoxThresh must be between 0 and 1");

            if (UnClipRatio <= 0)
                throw new ArgumentException("UnClipRatio must be > 0");

            if (BatchSize <= 0)
                throw new ArgumentException("BatchSize must be > 0");
        }
    }
}