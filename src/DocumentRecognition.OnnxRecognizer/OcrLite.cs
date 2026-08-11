/*
 * Modified by Zakirov-Ruslan
 * Original: https://github.com/RapidAI/RapidOCRCSharp
 */

using Ocr.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;


namespace Ocr
{
    /// <summary>
    /// Main class for Optical Character Recognition (OCR)
    /// Combines three neural network components: text detector, angle classifier, and character recognizer
    /// </summary>
    public partial class OcrLite
    {

        // Internal OCR components
        private TextDetector _dbNet;
        private TextLineOrientationClassifier _angleNet;
        private TextRecognizer _crnnNet;

        /// <summary>
        /// Debug mode - displays intermediate images during processing
        /// </summary>
        public bool IsDebugImg { get; set; }

        /// <summary>
        /// Display extracted text fragments
        /// </summary>
        public bool IsPartImg { get; set; }

        /// <summary>
        /// The path of the Det model file.
        /// </summary>
        public string DetPath { get; set; }

        /// <summary>
        /// The path of the Cls model file.
        /// </summary>
        public string ClsPath { get; set; }

        /// <summary>
        /// The path of the Rec model file.
        /// </summary>
        public string RecPath { get; set; }

        /// <summary>
        /// Path to the character dictionary file (keys)
        /// </summary>
        public string KeyDicPath { get; set; }

        /// <summary>
        /// Number of CPU threads for processing.
        /// Default is 70% of the total logical CPU cores
        /// </summary>
        public int ThreadNum { get; set; } = (int)(Environment.ProcessorCount * 0.7);

        /// <summary>
        /// Default constructor.
        /// Creates instances of all internal neural networks
        /// </summary>
        public OcrLite()
        {
            _dbNet = new TextDetector();
            _angleNet = new TextLineOrientationClassifier();
            _crnnNet = new TextRecognizer();
        }

        /// <summary>
        /// Constructor with model file paths
        /// </summary>
        /// <param name="detPath">The path of the Det model file.</param>
        /// <param name="clsPath">The path of the Rec model file.</param>
        /// <param name="recPath">The path of the Rec model file.</param>
        /// <param name="keysPath">The path of the Key dictionary file.</param>
        public OcrLite(string detPath, string clsPath, string recPath, string keysPath) : this()
        {
            InitModelFiles(detPath, clsPath, recPath, keysPath);
            ThrowIfModelFilesDoesntExists();
        }

        /// <summary>
        /// Asynchronous initialization of the OCR engine using previously set paths
        /// </summary>
        /// <returns>Task</returns>
        public async Task InitModels()
        {
            await InitModels(DetPath, ClsPath, RecPath, KeyDicPath, 0);
        }

        /// <summary>
        /// Asynchronous initialization using ModeOptions object
        /// </summary>
        /// <param name="options">ModeOptions object with model parameters</param>
        /// <returns>Task</returns>
        public async Task InitModels(ModeOptions options)
        {
            await InitModels(options.DetPath, options.ClsPath, options.RecPath, options.KeyDicPath, options.ThreadNum);
        }

        /// <summary>
        /// Complete asynchronous initialization of the OCR engine
        /// Loads all three models and the dictionary into memory
        /// </summary>
        /// <param name="detPath">Path to detector model</param>
        /// <param name="clsPath">Path to angle classifier model</param>
        /// <param name="recPath">Path to recognition model</param>
        /// <param name="keysPath">Path to character dictionary file</param>
        /// <param name="numThread">Number of CPU threads (0 - auto-detect)</param>
        /// <returns>Task</returns>
        public async Task InitModels(string detPath, string clsPath, string recPath, string keysPath, int numThread = 0)
        {
            try
            {
                InitModelFiles(detPath, clsPath, recPath, keysPath);
                ThrowIfModelFilesDoesntExists();

                if (numThread > 0)
                {
                    ThreadNum = numThread;
                }

                await _dbNet.InitModel(DetPath, ThreadNum);
                await _angleNet.InitModel(ClsPath, ThreadNum);
                await _crnnNet.InitModel(RecPath, keysPath, ThreadNum);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Internal method to save model file paths
        /// </summary>
        private void InitModelFiles(string detPath, string clsPath, string recPath, string keysPath)
        {
            DetPath = detPath;
            ClsPath = clsPath;
            RecPath = recPath;
            KeyDicPath = keysPath;
        }

        /// <summary>
        /// Checks if all model files exist
        /// Throws an exception if any file is missing
        /// </summary>
        /// <exception cref="Exception">Thrown when any model file is missing</exception>
        private void ThrowIfModelFilesDoesntExists()
        {
            _ = File.Exists(DetPath) ? true : throw new Exception("The det model file does not exist.");
            _ = File.Exists(ClsPath) ? true : throw new Exception("The cls model file does not exist.");
            _ = File.Exists(RecPath) ? true : throw new Exception("The rec model file does not exist.");
            _ = File.Exists(KeyDicPath) ? true : throw new Exception("The key dictionary file does not exist.");
        }

        /// <summary>
        /// Main recognition method using image file path
        /// </summary>
        /// <param name="imgFile">Path to image file</param>
        /// <param name="options">Detection options</param>
        /// <returns>OcrResult object with recognition results</returns>
        public OcrResult Detect(string imgFile, DetectOptions options)
        {
            options.Validate();
            using (Mat originSrc = Cv2.ImRead(imgFile, ImreadModes.Color))
            {
                return DetectCore(originSrc, options);
            }
        }

        /// <summary>
        /// Main recognition method using mat
        /// </summary>
        /// <param name="imgFile">Path to image file</param>
        /// <param name="options">Detection options</param>
        /// <returns>OcrResult object with recognition results</returns>
        public OcrResult Detect(Mat srcMat, DetectOptions options)
        {
            options.Validate();

            return DetectCore(srcMat, options);
        }

        /// <summary>
        /// Recognition method using image byte array
        /// </summary>
        /// <param name="imageBytes">Image byte array</param>
        /// <param name="options">Detection options</param>
        /// <returns>OcrResult object with recognition results</returns>
        public OcrResult Detect(byte[] imageBytes, DetectOptions options)
        {
            options.Validate();

            Mat originSrc = new Mat();
            originSrc = Cv2.ImDecode(imageBytes, ImreadModes.Color);

            return DetectCore(originSrc, options);
        }

        /// <summary>
        /// Recognition method using data stream
        /// </summary>
        /// <param name="stream">Stream with image data</param>
        /// <param name="options">Detection options</param>
        /// <returns>OcrResult object with recognition results</returns>
        public OcrResult Detect(Stream stream, DetectOptions options)
        {
            options.Validate();

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            Mat originSrc = new Mat();
            Cv2.ImDecode(originSrc, ImreadModes.Color);

            return DetectCore(originSrc, options);
        }

        /// <summary>
        /// Internal core method that prepares the image and calls the main recognition pipeline
        /// Applies padding, calculates scaling, and prepares the image for processing
        /// </summary>
        /// <param name="options">Detection options</param>
        /// <returns>OcrResult object with recognition results</returns>
        private OcrResult DetectCore(Mat originSrc, DetectOptions options)
        {
            int originMaxSide = Math.Max(originSrc.Cols, originSrc.Rows);

            int resize = options.MaxSideLen <= 0 || options.MaxSideLen > originMaxSide
                ? originMaxSide
                : options.MaxSideLen;

            resize += 2 * options.Padding;
            Rect paddingRect = new Rect(options.Padding, options.Padding, originSrc.Cols, originSrc.Rows);
            Mat paddingSrc = OcrUtils.MakePadding(originSrc, options.Padding);

            ScaleParam scale = ScaleParam.GetScaleParam(paddingSrc, resize);

            return DetectCore(paddingSrc, paddingRect, scale, options);
        }

        /// <summary>
        /// Internal core method that performs the entire recognition process
        /// Sequentially executes steps:
        /// 1. Text area detection (DbNet)
        /// 2. Drawing boxes on the image
        /// 3. Extracting text fragments
        /// 4. Determining text orientation (AngleNet)
        /// 5. Character recognition (CrnnNet)
        /// 6. Building the result
        /// </summary>
        /// <param name="src">Source image (Mat)</param>
        /// <param name="originRect">Original image rectangle</param>
        /// <param name="scale">Scaling parameters</param>
        /// <param name="options">Detection options</param>
        /// <returns>OcrResult object with recognition results</returns>
        private OcrResult DetectCore(Mat src, Rect originRect, ScaleParam scale, DetectOptions options)
        {
            Mat textBoxPaddingImg = src.Clone();
            int thickness = OcrUtils.GetThickness(src);
            Console.WriteLine("=====Start detect=====");
            var startTicks = DateTime.Now.Ticks;

            Console.WriteLine("---------- step: dbNet getTextBoxes ----------");
            var textBoxes = _dbNet.GetTextBoxes(src, scale, options.BoxScoreThresh, options.BoxThresh, options.UnClipRatio);
            var dbNetTime = (DateTime.Now.Ticks - startTicks) / 10000F;

            if (options.OnlyHorizontalLines)
            {
                textBoxes = OcrUtils.MergeVerticalWithHorizontalLines(textBoxes);
            }

            Console.WriteLine($"TextBoxesSize({textBoxes.Count})");
            textBoxes.ForEach(x => Console.WriteLine(x));

            Console.WriteLine("---------- step: drawTextBoxes ----------");
            OcrUtils.DrawTextBoxes(textBoxPaddingImg, textBoxes, thickness);

            //---------- getPartImages ----------
            List<Mat> partImages = OcrUtils.GetPartImages(src, textBoxes);
            if (IsPartImg)
            {
                for (int i = 0; i < partImages.Count; i++)
                {
                    Cv2.ImShow("PartImg({ i})", partImages[i]);
                }
            }

            Console.WriteLine("---------- step: angleNet getAngles ----------");
            //List<Angle> angles = _angleNet.GetAngles(partImages, options.DoAngle, options.MostAngle);

            List<Angle> angles = options.UseBatching
                ? _angleNet.GetAnglesBatched(partImages, options.DoAngle, options.MostAngle, options.BatchSize)
                : _angleNet.GetAngles(partImages, options.DoAngle, options.MostAngle);

            // Rotate partImgs
            for (int i = 0; i < partImages.Count; ++i)
            {
                if (angles[i].Index == 1)
                {
                    partImages[i] = OcrUtils.MatRotateClockWise180(partImages[i]);
                }

                if (IsDebugImg)
                {
                    Cv2.ImShow("PartImg({ i})", partImages[i]);
                }
            }

            Console.WriteLine("---------- step: crnnNet getTextLines ----------");
            //List<TextLine> textLines = _crnnNet.GetTextLines(partImages);

            List<TextLine> textLines = options.UseBatching
                ? _crnnNet.GetTextLinesBatched(partImages, options.BatchSize)
                : _crnnNet.GetTextLines(partImages);

            List<TextBlock> textBlocks = new List<TextBlock>();
            for (int i = 0; i < textLines.Count; ++i)
            {
                TextBlock textBlock = new TextBlock();
                textBlock.BoxPoints = textBoxes[i].Points;
                textBlock.BoxScore = textBoxes[i].Score;
                textBlock.AngleIndex = angles[i].Index;
                textBlock.AngleScore = angles[i].Score;
                textBlock.AngleTime = angles[i].Time;
                textBlock.Text = textLines[i].Text;
                textBlock.CharScores = textLines[i].CharScores;
                textBlock.CrnnTime = textLines[i].Time;
                textBlock.BlockTime = angles[i].Time + textLines[i].Time;
                textBlocks.Add(textBlock);
            }

            var endTicks = DateTime.Now.Ticks;
            var fullDetectTime = (endTicks - startTicks) / 10000F;

            // Cropped to original size
            Mat boxImg = new Mat(textBoxPaddingImg, originRect);

            StringBuilder strRes = new StringBuilder();
            textBlocks.ForEach(x => strRes.AppendLine(x.Text));

            OcrResult ocrResult = new OcrResult();
            ocrResult.TextBlocks = textBlocks;
            ocrResult.DbNetTime = dbNetTime;
            ocrResult.BoxImg = boxImg;
            ocrResult.DetectTime = fullDetectTime;
            ocrResult.StrRes = strRes.ToString();

            return ocrResult;
        }

    }
}
