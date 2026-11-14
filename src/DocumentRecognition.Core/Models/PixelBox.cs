using System;

namespace DocumentRecognition.Core.Models
{
    /// <summary>
    /// Прямоугольник в пиксельных координатах обработанного изображения.
    /// </summary>
    /// <remarks>
    /// Собственный тип, а не <c>OpenCvSharp.Rect</c>/<c>System.Drawing.Rectangle</c>,
    /// чтобы модель результата не зависела от библиотек обработки изображений.
    /// Координаты задаются в системе полного (ориентированного) изображения,
    /// которое возвращается в <c>DrawingRecognitionResult.DebugImage</c>.
    /// </remarks>
    public readonly struct PixelBox : IEquatable<PixelBox>
    {
        public PixelBox(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; }

        public int Y { get; }

        public int Width { get; }

        public int Height { get; }

        public int Right => X + Width;

        public int Bottom => Y + Height;

        /// <summary>
        /// Площадь в квадратных пикселях. Используется для выбора наиболее
        /// специфичной (вложенной) области при попадании курсора.
        /// </summary>
        public long Area => (long)Width * Height;

        public bool IsEmpty => Width <= 0 || Height <= 0;

        public bool Contains(int x, int y)
        {
            return x >= X && x < Right && y >= Y && y < Bottom;
        }

        public bool Contains(PixelBox other)
        {
            return other.X >= X && other.Y >= Y && other.Right <= Right && other.Bottom <= Bottom;
        }

        public bool IntersectsWith(PixelBox other)
        {
            return other.X < Right && X < other.Right && other.Y < Bottom && Y < other.Bottom;
        }

        public PixelBox Offset(int dx, int dy)
        {
            return new PixelBox(X + dx, Y + dy, Width, Height);
        }

        public bool Equals(PixelBox other)
        {
            return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is PixelBox other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = X;
                hash = (hash * 397) ^ Y;
                hash = (hash * 397) ^ Width;
                hash = (hash * 397) ^ Height;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"x={X}, y={Y}, w={Width}, h={Height}";
        }
    }
}
