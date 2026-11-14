/*
 * Modified by Zakirov-Ruslan
 * Original: https://github.com/RapidAI/RapidOCRCSharp
 */

using OpenCvSharp;
using System;

namespace Ocr.Models
{
    class ScaleParam
    {
        private int _srcWidth;
        private int _srcHeight;
        private int _dstWidth;
        private int _dstHeight;
        private float _scaleWidth;
        private float _scaleHeight;

        public int SrcWidth { get => _srcWidth; set => _srcWidth = value; }
        public int SrcHeight { get => _srcHeight; set => _srcHeight = value; }
        public int DstWidth { get => _dstWidth; set => _dstWidth = value; }
        public int DstHeight { get => _dstHeight; set => _dstHeight = value; }
        public float ScaleWidth { get => _scaleWidth; set => _scaleWidth = value; }
        public float ScaleHeight { get => _scaleHeight; set => _scaleHeight = value; }

        public ScaleParam(int srcWidth, int srcHeight, int dstWidth, int dstHeight, float scaleWidth, float scaleHeight)
        {
            _srcWidth = srcWidth;
            _srcHeight = srcHeight;
            _dstWidth = dstWidth;
            _dstHeight = dstHeight;
            _scaleWidth = scaleWidth;
            _scaleHeight = scaleHeight;
        }

        public static ScaleParam GetScaleParam(Mat src, int dstSize)
        {
            int srcWidth, srcHeight, dstWidth, dstHeight;
            srcWidth = src.Cols;
            dstWidth = src.Cols;
            srcHeight = src.Rows;
            dstHeight = src.Rows;

            float scale = 1.0F;
            if (dstWidth > dstHeight)
            {
                scale = (float)dstSize / (float)dstWidth;
                dstWidth = dstSize;
                dstHeight = (int)((float)dstHeight * scale);
            }
            else
            {
                scale = (float)dstSize / (float)dstHeight;
                dstHeight = dstSize;
                dstWidth = (int)((float)dstWidth * scale);
            }
            if (dstWidth % 32 != 0)
            {
                dstWidth = (dstWidth / 32 - 1) * 32;
                dstWidth = Math.Max(dstWidth, 32);
            }
            if (dstHeight % 32 != 0)
            {
                dstHeight = (dstHeight / 32 - 1) * 32;
                dstHeight = Math.Max(dstHeight, 32);
            }
            float scaleWidth = (float)dstWidth / (float)srcWidth;
            float scaleHeight = (float)dstHeight / (float)srcHeight;
            return new ScaleParam(srcWidth, srcHeight, dstWidth, dstHeight, scaleWidth, scaleHeight);
        }

        public override string ToString()
        {
            return $"sw:{this._srcWidth},sh:{this._srcHeight},dw:{this._dstWidth},dh:{this._dstHeight},{this._scaleWidth},{this._scaleHeight}";
        }
    }
}
