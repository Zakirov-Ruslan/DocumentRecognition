using DocumentRecognition.Core.Enums;
using DocumentRecognition.Core.Models;
using DocumentRecognition.Core.Models.Results;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Text;

namespace DocumentRecognition.WindowsForms
{
    /// <summary>
    /// Контрол просмотра изображения результата распознавания.
    /// </summary>
    /// <remarks>
    /// Возможности:
    /// <list type="bullet">
    /// <item>зум колесом мыши относительно курсора и панорамирование перетаскиванием;</item>
    /// <item>вписывание изображения в контрол (<c>Ctrl+0</c>);</item>
    /// <item>подсветка областей результата распознавания при наведении и выборе;</item>
    /// <item>тултип с координатами, уверенностью и распознанным текстом области.</item>
    /// </list>
    /// Координаты областей заданы в пикселях изображения и пересчитываются в экранные
    /// через текущий масштаб и смещение, поэтому корректно работают при любом зуме.
    /// </remarks>
    public sealed class ImageViewerControl : Control
    {
        private const float MinZoomFactor = 0.05f;
        private const float MaxZoomFactor = 40f;
        private const float WheelZoomStep = 1.2f;
        private const float KeyboardZoomStep = 1.25f;
        private const float KeyboardPanStep = 48f;
        private const int PanThresholdPixels = 3;
        private const int ToolTipDurationMs = 30000;
        private const int ToolTipTextLimit = 500;

        private readonly List<RecognitionRegion> _regions = new List<RecognitionRegion>();
        private readonly ToolTip _toolTip;

        private Bitmap? _image;
        private int _imageDpi;

        private float _scale = 1f;
        private float _fitScale = 1f;
        private PointF _offset;
        private bool _autoFit = true;

        private bool _isPanning;
        private bool _dragMoved;
        private Point _dragAnchor;
        private PointF _dragAnchorOffset;

        private RecognitionRegion? _hoveredRegion;
        private RecognitionRegion? _selectedRegion;
        private string? _toolTipText;

        private bool _showLayoutBlocks = true;
        private bool _showTitleBlockCells = true;
        private bool _showOcrLines = true;
        private bool _showTableCells = true;
        private BorderStyle _borderStyle = BorderStyle.FixedSingle;

        public ImageViewerControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable,
                true);

            BackColor = Color.FromArgb(64, 64, 64);
            TabStop = true;
            UpdateStyles();

            _toolTip = new ToolTip
            {
                InitialDelay = 0,
                ReshowDelay = 0,
                AutoPopDelay = ToolTipDurationMs,
                ShowAlways = true,
                UseFading = false,
                UseAnimation = false
            };
        }

        /// <summary>
        /// Событие возникает при смене области под курсором (в том числе при уходе курсора).
        /// </summary>
        public event EventHandler<RecognitionRegionEventArgs>? HoveredRegionChanged;

        /// <summary>
        /// Событие возникает при выборе области кликом (или сбросе выбора).
        /// </summary>
        public event EventHandler<RecognitionRegionEventArgs>? RegionSelected;

        /// <summary>
        /// Текущая выбранная (закреплённая) область.
        /// </summary>
        [Browsable(false)]
        public RecognitionRegion? SelectedRegion => _selectedRegion;

        /// <summary>
        /// Области, отображаемые поверх изображения.
        /// </summary>
        [Browsable(false)]
        public IReadOnlyList<RecognitionRegion> Regions => _regions;

        [Browsable(false)]
        public bool HasImage => _image != null;

        /// <summary>
        /// Отображать блоки разметки чертежа.
        /// </summary>
        [DefaultValue(true)]
        public bool ShowLayoutBlocks
        {
            get => _showLayoutBlocks;
            set => SetRegionVisibility(ref _showLayoutBlocks, value);
        }

        /// <summary>
        /// Отображать ячейки шаблонной сетки основной надписи.
        /// </summary>
        [DefaultValue(true)]
        public bool ShowTitleBlockCells
        {
            get => _showTitleBlockCells;
            set => SetRegionVisibility(ref _showTitleBlockCells, value);
        }

        /// <summary>
        /// Отображать OCR-строки.
        /// </summary>
        [DefaultValue(true)]
        public bool ShowOcrLines
        {
            get => _showOcrLines;
            set => SetRegionVisibility(ref _showOcrLines, value);
        }

        /// <summary>
        /// Отображать ячейки найденных таблиц.
        /// </summary>
        [DefaultValue(true)]
        public bool ShowTableCells
        {
            get => _showTableCells;
            set => SetRegionVisibility(ref _showTableCells, value);
        }

        /// <summary>
        /// Стиль рамки контрола (у <see cref="Control"/> такого свойства нет).
        /// </summary>
        [DefaultValue(BorderStyle.FixedSingle)]
        public BorderStyle BorderStyle
        {
            get => _borderStyle;
            set
            {
                if (_borderStyle == value)
                    return;

                _borderStyle = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Загружает изображение из массива байтов вместе с областями результата.
        /// </summary>
        /// <param name="imageBytes">Закодированное изображение (JPEG/PNG).</param>
        /// <param name="regions">Области результата распознавания.</param>
        /// <param name="dpi">DPI, с которым изображение было получено (для перевода в мм).</param>
        public void LoadImage(byte[]? imageBytes, IReadOnlyList<RecognitionRegion>? regions, int dpi = 0)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                Clear();
                return;
            }

            using var stream = new MemoryStream(imageBytes, writable: false);
            using var decoded = Image.FromStream(stream);

            SetImage(new Bitmap(decoded), regions, dpi);
        }

        /// <summary>
        /// Заменяет отображаемое изображение и области результата.
        /// Контрол забирает владение переданным <paramref name="image"/> и освобождает его при замене.
        /// </summary>
        public void SetImage(Bitmap? image, IReadOnlyList<RecognitionRegion>? regions, int dpi = 0)
        {
            if (!ReferenceEquals(_image, image))
            {
                _image?.Dispose();
                _image = image;
            }

            _imageDpi = dpi;

            _regions.Clear();
            if (regions != null)
                _regions.AddRange(regions);

            _hoveredRegion = null;
            _selectedRegion = null;
            HideToolTip();

            if (_image == null)
            {
                Invalidate();
                HoveredRegionChanged?.Invoke(this, new RecognitionRegionEventArgs(null));
                RegionSelected?.Invoke(this, new RecognitionRegionEventArgs(null));
                return;
            }

            _autoFit = true;
            ZoomToFit();
            Invalidate();
        }

        /// <summary>
        /// Очищает изображение и области результата.
        /// </summary>
        public void Clear()
        {
            _image?.Dispose();
            _image = null;
            _regions.Clear();
            _hoveredRegion = null;
            _selectedRegion = null;
            HideToolTip();
            Invalidate();
            HoveredRegionChanged?.Invoke(this, new RecognitionRegionEventArgs(null));
            RegionSelected?.Invoke(this, new RecognitionRegionEventArgs(null));
        }

        /// <summary>
        /// Вписывает изображение в контрол.
        /// </summary>
        public void ZoomToFit()
        {
            if (_image == null)
                return;

            _scale = CalculateFitScale();
            _fitScale = _scale;
            OffsetForCenteredView();
            _autoFit = true;
            Invalidate();
        }

        /// <summary>
        /// Выбирает область по ключу (например, при выборе строки в таблице результатов).
        /// Событие <see cref="RegionSelected"/> при этом не возникает, чтобы избежать рекурсии.
        /// </summary>
        public void SelectRegionByKey(string? key)
        {
            if (string.IsNullOrEmpty(key))
            {
                ApplySelection(null, raiseEvent: false, ensureVisible: false);
                return;
            }

            var region = _regions.FirstOrDefault(
                r => string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase));

            ApplySelection(region, raiseEvent: false, ensureVisible: true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.Clear(BackColor);

            if (_image == null)
            {
                DrawEmptyState(graphics);
                DrawBorder(graphics);
                return;
            }

            graphics.CompositingQuality = CompositingQuality.HighSpeed;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.InterpolationMode = _scale >= 2f
                ? InterpolationMode.NearestNeighbor
                : InterpolationMode.HighQualityBilinear;

            var destination = new RectangleF(
                _offset.X,
                _offset.Y,
                _image.Width * _scale,
                _image.Height * _scale);

            graphics.DrawImage(_image, destination);

            DrawRegions(graphics);
            DrawZoomLabel(graphics);
            DrawBorder(graphics);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (_image == null)
                return;

            if (_autoFit)
            {
                ZoomToFit();
                return;
            }

            _fitScale = CalculateFitScale();
            ConstrainOffset();
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Focus();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (_isPanning)
                return;

            SetHoveredRegion(null);
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (_image == null)
                return;

            var factor = e.Delta > 0 ? WheelZoomStep : 1f / WheelZoomStep;
            ZoomAt(e.Location, factor);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            if (_image == null)
                return;

            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
            {
                _isPanning = true;
                _dragMoved = false;
                _dragAnchor = e.Location;
                _dragAnchorOffset = _offset;
                Cursor = Cursors.SizeAll;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_image == null)
                return;

            if (_isPanning)
            {
                var dx = e.X - _dragAnchor.X;
                var dy = e.Y - _dragAnchor.Y;

                if (!_dragMoved && (Math.Abs(dx) > PanThresholdPixels || Math.Abs(dy) > PanThresholdPixels))
                {
                    _dragMoved = true;
                    SetHoveredRegion(null);
                }

                if (_dragMoved)
                {
                    _offset = new PointF(_dragAnchorOffset.X + dx, _dragAnchorOffset.Y + dy);
                    ConstrainOffset();
                    HideToolTip();
                    Invalidate();
                }

                return;
            }

            UpdateHover(e.Location);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (!_isPanning)
                return;

            _isPanning = false;

            if (_dragMoved || e.Button != MouseButtons.Left)
            {
                Cursor = _hoveredRegion != null ? Cursors.Hand : Cursors.Default;
                return;
            }

            var region = HitTest(e.Location);
            ApplySelection(region, raiseEvent: true, ensureVisible: false);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if (_image == null || e.Button != MouseButtons.Left)
                return;

            if (_scale > _fitScale * 1.5f)
                ZoomToFit();
            else
                ZoomAt(e.Location, 2f);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (_image == null)
                return;

            if ((e.Control && e.KeyCode == Keys.D0) || (e.Control && e.KeyCode == Keys.NumPad0))
            {
                ZoomToFit();
                e.Handled = true;
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Add:
                case Keys.Oemplus:
                    ZoomAt(new Point(ClientSize.Width / 2, ClientSize.Height / 2), KeyboardZoomStep);
                    e.Handled = true;
                    break;
                case Keys.Subtract:
                case Keys.OemMinus:
                    ZoomAt(new Point(ClientSize.Width / 2, ClientSize.Height / 2), 1f / KeyboardZoomStep);
                    e.Handled = true;
                    break;
                case Keys.Left:
                    PanBy(-KeyboardPanStep, 0);
                    e.Handled = true;
                    break;
                case Keys.Right:
                    PanBy(KeyboardPanStep, 0);
                    e.Handled = true;
                    break;
                case Keys.Up:
                    PanBy(0, -KeyboardPanStep);
                    e.Handled = true;
                    break;
                case Keys.Down:
                    PanBy(0, KeyboardPanStep);
                    e.Handled = true;
                    break;
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            var keyCode = keyData & Keys.KeyCode;

            if (keyCode == Keys.Left || keyCode == Keys.Right || keyCode == Keys.Up || keyCode == Keys.Down)
                return true;

            if (keyCode == Keys.Add || keyCode == Keys.Oemplus || keyCode == Keys.Subtract || keyCode == Keys.OemMinus)
                return true;

            return base.IsInputKey(keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip.Dispose();
                _image?.Dispose();
                _image = null;
            }

            base.Dispose(disposing);
        }

        private void SetRegionVisibility(ref bool field, bool value)
        {
            if (field == value)
                return;

            field = value;
            Invalidate();
        }

        private float CalculateFitScale()
        {
            if (_image == null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return 1f;

            var scaleX = (float)ClientSize.Width / _image.Width;
            var scaleY = (float)ClientSize.Height / _image.Height;

            return Math.Min(scaleX, scaleY);
        }

        private void OffsetForCenteredView()
        {
            if (_image == null)
                return;

            _offset = new PointF(
                (ClientSize.Width - _image.Width * _scale) / 2f,
                (ClientSize.Height - _image.Height * _scale) / 2f);
        }

        private void ZoomAt(Point clientPoint, float factor)
        {
            if (_image == null)
                return;

            var fit = CalculateFitScale();
            var minScale = Math.Max(MinZoomFactor, fit * 0.25f);
            var maxScale = Math.Max(fit * MaxZoomFactor, minScale * 2f);

            var newScale = Math.Clamp(_scale * factor, minScale, maxScale);

            if (Math.Abs(newScale - _scale) < 0.0001f)
                return;

            var ratio = newScale / _scale;

            _offset = new PointF(
                clientPoint.X - (clientPoint.X - _offset.X) * ratio,
                clientPoint.Y - (clientPoint.Y - _offset.Y) * ratio);

            _scale = newScale;
            _fitScale = fit;
            _autoFit = false;
            ConstrainOffset();
            UpdateHover(clientPoint);
            Invalidate();
        }

        private void PanBy(float dx, float dy)
        {
            _offset = new PointF(_offset.X + dx, _offset.Y + dy);
            _autoFit = false;
            ConstrainOffset();
            Invalidate();
        }

        private void ConstrainOffset()
        {
            if (_image == null)
                return;

            _offset = new PointF(
                ClampAxis(_offset.X, ClientSize.Width, _image.Width * _scale),
                ClampAxis(_offset.Y, ClientSize.Height, _image.Height * _scale));
        }

        private static float ClampAxis(float offset, float viewSize, float contentSize)
        {
            // Изображение меньше окна — центрируем; иначе не даём утащить его за край.
            if (contentSize <= viewSize)
                return (viewSize - contentSize) / 2f;

            return Math.Clamp(offset, viewSize - contentSize, 0f);
        }

        private void UpdateHover(Point location)
        {
            var region = HitTest(location);
            SetHoveredRegion(region);
            UpdateToolTip(location);
        }

        private void SetHoveredRegion(RecognitionRegion? region)
        {
            if (ReferenceEquals(region, _hoveredRegion))
                return;

            _hoveredRegion = region;
            Cursor = region != null ? Cursors.Hand : (_isPanning ? Cursors.SizeAll : Cursors.Default);

            if (region == null)
                HideToolTip();

            HoveredRegionChanged?.Invoke(this, new RecognitionRegionEventArgs(region));
            Invalidate();
        }

        private void ApplySelection(RecognitionRegion? region, bool raiseEvent, bool ensureVisible)
        {
            if (ReferenceEquals(region, _selectedRegion))
                return;

            _selectedRegion = region;

            if (ensureVisible && region != null)
                EnsureVisible(region.Box);

            if (raiseEvent)
                RegionSelected?.Invoke(this, new RecognitionRegionEventArgs(region));

            Invalidate();
        }

        private void EnsureVisible(PixelBox box)
        {
            if (_image == null)
                return;

            var rect = ImageToClient(box);
            var viewport = new RectangleF(0, 0, ClientSize.Width, ClientSize.Height);

            if (viewport.Contains(rect))
                return;

            var centerX = rect.X + rect.Width / 2f;
            var centerY = rect.Y + rect.Height / 2f;

            _offset = new PointF(
                _offset.X + ClientSize.Width / 2f - centerX,
                _offset.Y + ClientSize.Height / 2f - centerY);

            ConstrainOffset();
        }

        private RecognitionRegion? HitTest(Point clientPoint)
        {
            if (_image == null)
                return null;

            var imagePoint = ClientToImage(clientPoint);
            var x = (int)imagePoint.X;
            var y = (int)imagePoint.Y;

            RecognitionRegion? best = null;

            foreach (var region in _regions)
            {
                if (!IsRegionVisible(region.Kind) || !region.Box.Contains(x, y))
                    continue;

                // Из вложенных областей выбираем самую маленькую — она точнее.
                if (best == null || region.Box.Area < best.Box.Area)
                    best = region;
            }

            return best;
        }

        private bool IsRegionVisible(RecognitionRegionKind kind)
        {
            switch (kind)
            {
                case RecognitionRegionKind.LayoutBlock:
                    return _showLayoutBlocks;
                case RecognitionRegionKind.TitleBlockCell:
                    return _showTitleBlockCells;
                case RecognitionRegionKind.OcrLine:
                    return _showOcrLines;
                case RecognitionRegionKind.TableCell:
                    return _showTableCells;
                default:
                    return true;
            }
        }

        private RectangleF ImageToClient(PixelBox box)
        {
            return new RectangleF(
                _offset.X + box.X * _scale,
                _offset.Y + box.Y * _scale,
                box.Width * _scale,
                box.Height * _scale);
        }

        private PointF ClientToImage(Point point)
        {
            return new PointF(
                (point.X - _offset.X) / _scale,
                (point.Y - _offset.Y) / _scale);
        }

        private void DrawRegions(Graphics graphics)
        {
            var viewport = new RectangleF(0, 0, ClientSize.Width, ClientSize.Height);

            foreach (var region in _regions)
            {
                if (!IsRegionVisible(region.Kind))
                    continue;

                var rect = ImageToClient(region.Box);

                if (!rect.IntersectsWith(viewport))
                    continue;

                var isHovered = ReferenceEquals(region, _hoveredRegion);
                var isSelected = ReferenceEquals(region, _selectedRegion);

                // На мелком масштабе рамки превращаются в шум — показываем только активные.
                if (!isHovered && !isSelected && (rect.Width < 2f || rect.Height < 2f))
                    continue;

                var color = GetRegionColor(region.Kind);

                if (isHovered || isSelected)
                {
                    using var fill = new SolidBrush(Color.FromArgb(isSelected ? 70 : 40, color));
                    graphics.FillRectangle(fill, rect);
                }

                using var pen = new Pen(color, isSelected ? 3f : isHovered ? 2f : 1f);
                graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        private void DrawZoomLabel(Graphics graphics)
        {
            var percent = _fitScale > 0f ? _scale / _fitScale * 100f : 100f;
            var text = $"Масштаб: {percent:0}%   |   Колесо — зум, ЛКМ — панорама, Ctrl+0 — вписать";

            var size = TextRenderer.MeasureText(text, Font);
            var bounds = new Rectangle(
                Math.Max(4, ClientSize.Width - size.Width - 14),
                Math.Max(4, ClientSize.Height - size.Height - 12),
                size.Width + 4,
                size.Height + 2);

            using var background = new SolidBrush(Color.FromArgb(170, 0, 0, 0));
            graphics.FillRectangle(background, bounds);

            TextRenderer.DrawText(
                graphics,
                text,
                Font,
                bounds,
                Color.White,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private void DrawEmptyState(Graphics graphics)
        {
            const string text = "Выберите PDF-документ, чтобы увидеть результат распознавания.";

            var size = TextRenderer.MeasureText(text, Font);
            var bounds = new Rectangle(
                Math.Max(0, (ClientSize.Width - size.Width) / 2),
                Math.Max(0, (ClientSize.Height - size.Height) / 2),
                size.Width,
                size.Height);

            TextRenderer.DrawText(graphics, text, Font, bounds, Color.Gainsboro, TextFormatFlags.NoPrefix);
        }

        private void DrawBorder(Graphics graphics)
        {
            switch (_borderStyle)
            {
                case BorderStyle.FixedSingle:
                    ControlPaint.DrawBorder(
                        graphics,
                        ClientRectangle,
                        SystemColors.ControlDark,
                        ButtonBorderStyle.Solid);
                    break;
                case BorderStyle.Fixed3D:
                    ControlPaint.DrawBorder3D(graphics, ClientRectangle, Border3DStyle.Etched);
                    break;
            }
        }

        private void UpdateToolTip(Point location)
        {
            var region = _hoveredRegion;

            if (region == null)
            {
                HideToolTip();
                return;
            }

            var text = BuildToolTipText(region);

            if (string.Equals(text, _toolTipText, StringComparison.Ordinal))
                return;

            _toolTip.Hide(this);
            _toolTip.Show(text, this, location.X + 16, location.Y + 20, ToolTipDurationMs);
            _toolTipText = text;
        }

        private void HideToolTip()
        {
            if (_toolTipText == null)
                return;

            _toolTip.Hide(this);
            _toolTipText = null;
        }

        private string BuildToolTipText(RecognitionRegion region)
        {
            var builder = new StringBuilder();

            builder.AppendLine(GetKindTitle(region.Kind));

            if (!string.IsNullOrEmpty(region.Label))
                builder.AppendLine($"Блок: {region.Label}");

            if (!string.IsNullOrEmpty(region.Key) &&
                !string.Equals(region.Key, region.Label, StringComparison.Ordinal))
            {
                builder.AppendLine($"Ключ: {region.Key}");
            }

            if (region.Score > 0f)
                builder.AppendLine($"Уверенность: {region.Score:P0}");

            builder.AppendLine($"Координаты: X={region.Box.X}, Y={region.Box.Y}");
            builder.AppendLine($"Размер: {region.Box.Width}×{region.Box.Height} px");

            if (_imageDpi > 0)
            {
                const double mmPerInch = 25.4;
                var mmPerPx = mmPerInch / _imageDpi;

                builder.AppendLine(string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    "В мм: X={0:0.#}, Y={1:0.#}, W={2:0.#}, H={3:0.#}",
                    region.Box.X * mmPerPx,
                    region.Box.Y * mmPerPx,
                    region.Box.Width * mmPerPx,
                    region.Box.Height * mmPerPx));
            }

            if (!string.IsNullOrWhiteSpace(region.Text))
            {
                var text = region.Text.Trim();

                if (text.Length > ToolTipTextLimit)
                    text = text.Substring(0, ToolTipTextLimit) + "…";

                builder.AppendLine();
                builder.AppendLine("Распознанный текст:");
                builder.Append(text);
            }

            return builder.ToString().TrimEnd();
        }

        private static string GetKindTitle(RecognitionRegionKind kind)
        {
            switch (kind)
            {
                case RecognitionRegionKind.LayoutBlock:
                    return "Блок разметки";
                case RecognitionRegionKind.TitleBlockCell:
                    return "Ячейка основной надписи";
                case RecognitionRegionKind.OcrLine:
                    return "Распознанный текст";
                case RecognitionRegionKind.TableCell:
                    return "Ячейка таблицы";
                default:
                    return "Область";
            }
        }

        private static Color GetRegionColor(RecognitionRegionKind kind)
        {
            switch (kind)
            {
                case RecognitionRegionKind.LayoutBlock:
                    return Color.FromArgb(0, 220, 0);
                case RecognitionRegionKind.TitleBlockCell:
                    return Color.DodgerBlue;
                case RecognitionRegionKind.OcrLine:
                    return Color.Orange;
                case RecognitionRegionKind.TableCell:
                    return Color.MediumOrchid;
                default:
                    return Color.White;
            }
        }
    }

    /// <summary>
    /// Параметры события выбора/наведения области результата распознавания.
    /// </summary>
    public sealed class RecognitionRegionEventArgs : EventArgs
    {
        public RecognitionRegionEventArgs(RecognitionRegion? region)
        {
            Region = region;
        }

        /// <summary>
        /// Область, к которой относится событие, либо <c>null</c>, если область сброшена.
        /// </summary>
        public RecognitionRegion? Region { get; }
    }
}
