using Avalonia;
using Avalonia.Controls;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Avalonia.ImageCropper.Controls;

/// <summary>
/// The main crop area control that handles image display and crop selection.
/// </summary>
public partial class CropArea : UserControl
{
    #region Fields

    private Bitmap? _currentBitmap;
    private Bitmap? _fullResolutionBitmap;
    private bool _isDragging;
    private bool _isResizing;
    private string? _activeHandle;
    private Rect _cropArea;
    private Rect _dragStartCropArea;
    private Size _imageDisplaySize;
    private Point _imageDisplayOffset;
    private Point _pointerStartPosition;
    private double _rotationAngle;
    private double _rotationStartAngle;
    private double _rotationInitialAngle;
    private ImageCropperOptions _options = new();
    private DateTime _lastPreviewUpdate = DateTime.MinValue;

    private Grid? _backgroundPattern;
    private Image? _mainImage;
    private Grid? _cropOverlay;
    private AvaloniaPath? _overlayCutout;
    private Grid? _selectionGroup;
    private Border? _cropSelection;
    private Border? _handleTL, _handleTR, _handleBL, _handleBR;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the current original image path.
    /// </summary>
    public string? CurrentOriginalImagePath { get; private set; }

    /// <summary>
    /// Event raised when the image area is clicked (for file selection).
    /// </summary>
    public event EventHandler? ImageAreaClicked;

    /// <summary>
    /// Event raised when the crop area changes.
    /// </summary>
    public event EventHandler? CropChanged;

    #endregion

    #region Constructor

    public CropArea()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        InitializeControls();
        InitializeEventHandlers();
    }

    private void InitializeControls()
    {
        _backgroundPattern = this.FindControl<Grid>("BackgroundPattern");
        _mainImage = this.FindControl<Image>("MainImage");
        _cropOverlay = this.FindControl<Grid>("CropOverlay");
        _overlayCutout = this.FindControl<AvaloniaPath>("OverlayCutout");
        _selectionGroup = this.FindControl<Grid>("SelectionGroup");
        _cropSelection = this.FindControl<Border>("CropSelection");
        _handleTL = this.FindControl<Border>("HandleTopLeft");
        _handleTR = this.FindControl<Border>("HandleTopRight");
        _handleBL = this.FindControl<Border>("HandleBottomLeft");
        _handleBR = this.FindControl<Border>("HandleBottomRight");

        var mainArea = this.FindControl<Border>("MainImageArea");
        if (mainArea != null)
        {
            mainArea.PointerPressed += (s, e) =>
            {
                ImageAreaClicked?.Invoke(this, EventArgs.Empty);
            };
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the cropper options.
    /// </summary>
    public void SetOptions(ImageCropperOptions options)
    {
        _options = options;
        UpdateCropShape();
    }

    /// <summary>
    /// Loads an image from the specified path.
    /// </summary>
    public async Task LoadImageAsync(string path, CropSettings? settings = null)
    {
        if (!File.Exists(path)) return;

        CurrentOriginalImagePath = path;

        await using var fileStream = File.OpenRead(path);
        var bitmap = LoadBitmapWithExifOrientation(fileStream, path);

        _fullResolutionBitmap?.Dispose();
        _fullResolutionBitmap = bitmap;

        var resized = ResizeImageIfNeeded(bitmap);
        await LoadBitmapAsync(resized, settings);
    }

    /// <summary>
    /// Rotates the image by the specified degrees.
    /// </summary>
    public void Rotate(double degrees)
    {
        if (_currentBitmap == null || _fullResolutionBitmap == null) return;

        _rotationAngle += degrees;
        NormalizeRotationAngle();

        _currentBitmap = RotateBitmap(_currentBitmap, (int)degrees);
        _fullResolutionBitmap = RotateBitmap(_fullResolutionBitmap, (int)degrees);

        if (_mainImage != null && _currentBitmap != null)
        {
            _mainImage.Source = _currentBitmap;
        }

        UpdateCropSize();
        ApplyCropTransform();
        CropChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Resets the crop area to default.
    /// </summary>
    public void ResetCrop()
    {
        if (_currentBitmap != null)
        {
            UpdateCropSize();
            CropChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Resets the entire control state.
    /// </summary>
    public void Reset()
    {
        _currentBitmap?.Dispose();
        _currentBitmap = null;
        _fullResolutionBitmap?.Dispose();
        _fullResolutionBitmap = null;
        CurrentOriginalImagePath = null;

        if (_mainImage != null) _mainImage.IsVisible = false;
        if (_backgroundPattern != null) _backgroundPattern.IsVisible = true;
        if (_cropOverlay != null) _cropOverlay.IsVisible = false;
    }

    /// <summary>
    /// Creates a cropped image based on current settings.
    /// </summary>
    public Bitmap? CreateCroppedImage()
    {
        return CreateCroppedImageInternal(_options.OutputSize);
    }

    /// <summary>
    /// Creates a preview image (smaller, faster).
    /// </summary>
    public Bitmap? CreatePreviewImage()
    {
        return CreateCroppedImageInternal(140);
    }

    /// <summary>
    /// Gets the current crop settings.
    /// </summary>
    public CropSettings? GetCropSettings()
    {
        if (!IsCropStateValid()) return null;

        return new CropSettings
        {
            X = _cropArea.X,
            Y = _cropArea.Y,
            Width = _cropArea.Width,
            Height = _cropArea.Height,
            RotationAngle = _rotationAngle,
            ImageDisplayWidth = _imageDisplaySize.Width,
            ImageDisplayHeight = _imageDisplaySize.Height,
            ImageDisplayOffsetX = _imageDisplayOffset.X,
            ImageDisplayOffsetY = _imageDisplayOffset.Y
        };
    }

    #endregion

    #region Image Loading

    private async Task LoadBitmapAsync(Bitmap bitmap, CropSettings? settings = null)
    {
        _currentBitmap?.Dispose();
        _currentBitmap = bitmap;

        if (_mainImage == null || _backgroundPattern == null || _cropOverlay == null) return;

        _mainImage.Source = _currentBitmap;
        _mainImage.IsVisible = true;
        _backgroundPattern.IsVisible = false;
        _cropOverlay.IsVisible = true;

        await Task.Delay(100);

        if (settings != null && TryRestoreSettings(settings))
        {
            ApplyCropTransform();
        }
        else
        {
            UpdateCropSize();
        }

        CropChanged?.Invoke(this, EventArgs.Empty);
    }

    private Bitmap ResizeImageIfNeeded(Bitmap original)
    {
        var width = original.PixelSize.Width;
        var height = original.PixelSize.Height;
        var maxSize = _options.MaximumDisplayImageSize;

        if (width <= maxSize && height <= maxSize) return original;

        double scale = width > height ? (double)maxSize / width : (double)maxSize / height;
        var newWidth = (int)(width * scale);
        var newHeight = (int)(height * scale);

        return original.CreateScaledBitmap(new PixelSize(newWidth, newHeight), BitmapInterpolationMode.HighQuality);
    }

    private static Bitmap LoadBitmapWithExifOrientation(Stream stream, string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);
                var exifDir = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                // Note: EXIF orientation handling could be expanded here
            }
            catch { }
        }

        return new Bitmap(stream);
    }

    #endregion

    #region Crop Area Management

    private void UpdateCropSize()
    {
        if (_currentBitmap == null || _cropOverlay == null) return;

        var bounds = _cropOverlay.Bounds;
        if (bounds.Width == 0 || bounds.Height == 0)
        {
            SetDefaultCropValues();
            return;
        }

        CalculateDisplayMetrics(bounds);
        InitializeCrop();
        UpdateCropShape();
    }

    private void SetDefaultCropValues()
    {
        _cropArea = new Rect(100, 50, 200, 200);
        _imageDisplaySize = new Size(400, 300);
        _imageDisplayOffset = new Point(100, 50);
    }

    private void CalculateDisplayMetrics(Rect containerBounds)
    {
        var imageAspectRatio = (double)_currentBitmap!.PixelSize.Width / _currentBitmap.PixelSize.Height;
        var containerAspectRatio = containerBounds.Width / containerBounds.Height;

        if (imageAspectRatio > containerAspectRatio)
        {
            _imageDisplaySize = new Size(containerBounds.Width, containerBounds.Width / imageAspectRatio);
            var marginY = (containerBounds.Height - _imageDisplaySize.Height) / 2;
            _imageDisplayOffset = new Point(0, Math.Max(0, marginY));
        }
        else
        {
            _imageDisplaySize = new Size(containerBounds.Height * imageAspectRatio, containerBounds.Height);
            var marginX = (containerBounds.Width - _imageDisplaySize.Width) / 2;
            _imageDisplayOffset = new Point(Math.Max(0, marginX), 0);
        }
    }

    private void InitializeCrop()
    {
        if (_cropSelection == null) return;

        var maxSize = CalculateMaximumCropSize();
        var size = Math.Clamp(
            Math.Min(_imageDisplaySize.Width, _imageDisplaySize.Height) * 0.5,
            _options.MinimumCropSize,
            maxSize
        );

        _cropSelection.Width = size;
        _cropSelection.Height = size;
        _cropArea = new Rect(
            _imageDisplayOffset.X + (_imageDisplaySize.Width - size) / 2,
            _imageDisplayOffset.Y + (_imageDisplaySize.Height - size) / 2,
            size, size
        );
        _rotationAngle = 0;
        ApplyCropTransform();
    }

    private void UpdateCropShape()
    {
        if (_cropSelection == null) return;

        var radius = _options.CropShape == CropShape.Circle
            ? _cropArea.Width / 2
            : 0;
        _cropSelection.CornerRadius = new CornerRadius(radius);
    }

    private void ApplyCropTransform()
    {
        if (_selectionGroup == null || _cropSelection == null) return;

        var snapped = SnapToPixels(_cropArea);
        _selectionGroup.Width = snapped.Width;
        _selectionGroup.Height = snapped.Height;
        _selectionGroup.Margin = new Thickness(snapped.X, snapped.Y, 0, 0);
        _cropSelection.Width = snapped.Width;
        _cropSelection.Height = snapped.Height;

        UpdateCropShape();
        UpdateRotation();
        UpdateCutout();
    }

    private void UpdateCutout()
    {
        if (_cropOverlay == null || _overlayCutout == null || _cropOverlay.Bounds.Width == 0) return;

        var outerRect = SnapToPixels(new Rect(0, 0, _cropOverlay.Bounds.Width, _cropOverlay.Bounds.Height));
        var innerRect = SnapToPixels(_cropArea);
        var center = innerRect.Center;
        var radius = innerRect.Width / 2;

        Geometry innerGeometry = _options.CropShape == CropShape.Circle
            ? new EllipseGeometry { Center = center, RadiusX = radius, RadiusY = radius }
            : new RectangleGeometry(innerRect);

        _overlayCutout.Data = new GeometryGroup
        {
            FillRule = FillRule.EvenOdd,
            Children = { new RectangleGeometry(outerRect), innerGeometry }
        };
    }

    private void UpdateRotation()
    {
        if (_selectionGroup == null) return;
        _selectionGroup.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        _selectionGroup.RenderTransform = new RotateTransform(_rotationAngle);
        PositionHandles();
    }

    private void PositionHandles()
    {
        if (_selectionGroup == null) return;

        var width = _selectionGroup.Width;
        var center = width / 2;
        var radius = width / 2;
        var angles = new[] { 45.0, 135.0, 225.0, 315.0 };
        var handles = new[] { _handleTL, _handleTR, _handleBL, _handleBR };

        for (int i = 0; i < 4; i++)
        {
            var handle = handles[i];
            if (handle == null) continue;

            var angleRad = angles[i] * Math.PI / 180;
            var x = center + radius * Math.Cos(angleRad) - 7;
            var y = center + radius * Math.Sin(angleRad) - 7;
            Canvas.SetLeft(handle, x);
            Canvas.SetTop(handle, y);
        }
    }

    private double CalculateMaximumCropSize()
    {
        if (_imageDisplaySize.Width <= 0 || _imageDisplaySize.Height <= 0)
            return _options.MaximumCropSize;

        var maxFromImage = Math.Min(_imageDisplaySize.Width, _imageDisplaySize.Height) * _options.MaximumCropRatio;
        return Math.Min(_options.MaximumCropSize, maxFromImage);
    }

    private bool TryRestoreSettings(CropSettings settings)
    {
        if (_currentBitmap == null || _cropOverlay == null) return false;

        try
        {
            var bounds = _cropOverlay.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0) return false;

            CalculateDisplayMetrics(bounds);

            var scaleX = settings.ImageDisplayWidth > 0 ? _imageDisplaySize.Width / settings.ImageDisplayWidth : 1;
            var scaleY = settings.ImageDisplayHeight > 0 ? _imageDisplaySize.Height / settings.ImageDisplayHeight : 1;

            var relativeX = settings.X - settings.ImageDisplayOffsetX;
            var relativeY = settings.Y - settings.ImageDisplayOffsetY;

            var newX = _imageDisplayOffset.X + relativeX * scaleX;
            var newY = _imageDisplayOffset.Y + relativeY * scaleY;
            var newWidth = Math.Max(1, Math.Min(settings.Width * scaleX, _imageDisplaySize.Width));
            var newHeight = Math.Max(1, Math.Min(settings.Height * scaleY, _imageDisplaySize.Height));

            _cropArea = new Rect(newX, newY, newWidth, newHeight);
            _rotationAngle = settings.RotationAngle;

            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Event Handlers

    private void InitializeEventHandlers()
    {
        if (_cropSelection == null || _selectionGroup == null || _cropOverlay == null) return;

        _cropSelection.PointerPressed += OnCropSelectionPressed;
        _cropSelection.PointerMoved += OnCropSelectionMoved;
        _cropSelection.PointerReleased += OnCropSelectionReleased;

        _selectionGroup.PointerPressed += OnSelectionGroupPressed;
        _selectionGroup.PointerMoved += OnSelectionGroupMoved;
        _selectionGroup.PointerReleased += OnSelectionGroupReleased;

        _cropOverlay.PointerPressed += OnOverlayPressed;
        _cropOverlay.PointerMoved += OnOverlayMoved;
        _cropOverlay.PointerReleased += OnOverlayReleased;
        _cropOverlay.SizeChanged += (s, e) =>
        {
            if (_currentBitmap != null) UpdateCropSize();
        };

        AttachHandleEvents();
    }

    private void AttachHandleEvents()
    {
        var handles = new[] { (_handleTL, "tl"), (_handleTR, "tr"), (_handleBL, "bl"), (_handleBR, "br") };

        foreach (var (handle, position) in handles)
        {
            if (handle == null || _cropOverlay == null) continue;

            handle.PointerPressed += (s, e) =>
            {
                if (IsLeftButtonPressed(e, _cropOverlay!))
                {
                    _isResizing = true;
                    _activeHandle = position;
                    _pointerStartPosition = e.GetPosition(_cropOverlay);
                    _rotationStartAngle = CalculateAngle(_cropArea.Center, _pointerStartPosition);
                    _rotationInitialAngle = _rotationAngle;
                    e.Pointer.Capture(_cropOverlay);
                    e.Handled = true;
                }
            };

            handle.PointerReleased += (s, e) =>
            {
                EndDragAndResize();
                e.Pointer.Capture(null);
                e.Handled = true;
            };
        }
    }

    private void OnCropSelectionPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_cropSelection == null || _cropOverlay == null) return;
        if (!IsLeftButtonPressed(e, _cropSelection)) return;

        var position = e.GetPosition(_cropSelection);
        var center = new Point(_cropSelection.Width / 2, _cropSelection.Height / 2);
        var (distance, deltaX, deltaY) = CalculateDistanceAndDeltas(position, center);
        var radius = _cropSelection.Width / 2;

        if (IsOnResizeEdge(distance, radius))
        {
            StartResize(e, deltaX, deltaY);
        }
    }

    private void OnCropSelectionMoved(object? sender, PointerEventArgs e)
    {
        if (_cropSelection == null || _cropOverlay == null) return;

        if (!_isResizing)
        {
            UpdateCursor(e.GetPosition(_cropSelection), _cropSelection);
            return;
        }

        if (!ContinueDragOperation(e, _cropOverlay))
        {
            EndResize();
            return;
        }

        PerformResize(e.GetPosition(_cropOverlay));
        ApplyCropTransform();
        NotifyCropChanged();
        e.Handled = true;
    }

    private void OnCropSelectionReleased(object? sender, PointerReleasedEventArgs e)
    {
        EndResize();
        if (_cropSelection != null) _cropSelection.Cursor = new Cursor(StandardCursorType.SizeAll);
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnSelectionGroupPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_cropSelection == null || _cropOverlay == null) return;
        if (!IsLeftButtonPressed(e, _cropSelection)) return;
        StartDrag(e);
    }

    private void OnSelectionGroupMoved(object? sender, PointerEventArgs e)
    {
        if (_selectionGroup == null || _cropOverlay == null) return;

        if (!_isDragging)
        {
            _selectionGroup.Cursor = new Cursor(StandardCursorType.SizeAll);
            return;
        }

        if (!ContinueDragOperation(e, _cropOverlay))
        {
            EndDrag();
            return;
        }

        PerformDrag(e.GetPosition(_cropOverlay));
        NotifyCropChanged();
        e.Handled = true;
    }

    private void OnSelectionGroupReleased(object? sender, PointerReleasedEventArgs e)
    {
        EndDragAndResize();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnOverlayPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_cropOverlay == null) return;
        if (!IsLeftButtonPressed(e, _cropOverlay)) return;

        var position = e.GetPosition(_cropOverlay);
        var center = new Point(_cropArea.X + _cropArea.Width / 2, _cropArea.Y + _cropArea.Height / 2);
        var (distance, deltaX, deltaY) = CalculateDistanceAndDeltas(position, center);

        if (IsOnResizeEdge(distance, _cropArea.Width / 2))
        {
            StartResize(e, deltaX, deltaY);
        }
        else if (distance > _cropArea.Width / 2 + 10)
        {
            // Click outside the crop area - allow selecting a new image
            ImageAreaClicked?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnOverlayMoved(object? sender, PointerEventArgs e)
    {
        if (_cropOverlay == null) return;

        if (_isResizing && !string.IsNullOrEmpty(_activeHandle))
        {
            if (!ContinueDragOperation(e, _cropOverlay))
            {
                EndDragAndResize();
                return;
            }
            PerformRotation(e.GetPosition(_cropOverlay));
            NotifyCropChanged();
            e.Handled = true;
        }
        else if (_isResizing)
        {
            if (!ContinueDragOperation(e, _cropOverlay))
            {
                EndResize();
                return;
            }
            PerformResize(e.GetPosition(_cropOverlay));
            ApplyCropTransform();
            NotifyCropChanged();
            e.Handled = true;
        }
    }

    private void OnOverlayReleased(object? sender, PointerReleasedEventArgs e)
    {
        EndDragAndResize();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    #endregion

    #region Interaction Helpers

    private void StartResize(PointerPressedEventArgs e, double deltaX, double deltaY)
    {
        if (_cropOverlay == null || _cropSelection == null) return;
        _isResizing = true;
        _pointerStartPosition = e.GetPosition(_cropOverlay);
        _dragStartCropArea = _cropArea;
        _cropSelection.Cursor = new Cursor(GetResizeCursor(deltaX, deltaY));
        e.Pointer.Capture(_cropSelection);
        e.Handled = true;
    }

    private void StartDrag(PointerPressedEventArgs e)
    {
        if (_cropOverlay == null || _cropSelection == null) return;
        _isDragging = true;
        _pointerStartPosition = e.GetPosition(_cropOverlay);
        _dragStartCropArea = _cropArea;
        _cropSelection.Cursor = new Cursor(StandardCursorType.SizeAll);
        e.Pointer.Capture(_cropSelection);
        e.Handled = true;
    }

    private void EndResize() => _isResizing = false;
    private void EndDrag() => _isDragging = false;

    private void EndDragAndResize()
    {
        _isDragging = false;
        _isResizing = false;
        _activeHandle = null;
        CropChanged?.Invoke(this, EventArgs.Empty);
    }

    private void PerformDrag(Point currentPosition)
    {
        if (!IsCropStateValid()) return;

        var delta = currentPosition - _pointerStartPosition;
        var newX = Math.Clamp(
            _dragStartCropArea.X + delta.X,
            _imageDisplayOffset.X,
            _imageDisplayOffset.X + _imageDisplaySize.Width - _dragStartCropArea.Width
        );
        var newY = Math.Clamp(
            _dragStartCropArea.Y + delta.Y,
            _imageDisplayOffset.Y,
            _imageDisplayOffset.Y + _imageDisplaySize.Height - _dragStartCropArea.Height
        );

        _cropArea = new Rect(newX, newY, _dragStartCropArea.Width, _dragStartCropArea.Height);
        ApplyCropTransform();
    }

    private void PerformResize(Point currentPosition)
    {
        if (!IsCropStateValid()) return;

        var centerX = _dragStartCropArea.X + _dragStartCropArea.Width / 2;
        var centerY = _dragStartCropArea.Y + _dragStartCropArea.Height / 2;

        var startLength = Math.Max(1, Math.Sqrt(
            Math.Pow(_pointerStartPosition.X - centerX, 2) +
            Math.Pow(_pointerStartPosition.Y - centerY, 2)));

        var currentLength = Math.Sqrt(
            Math.Pow(currentPosition.X - centerX, 2) +
            Math.Pow(currentPosition.Y - centerY, 2));

        var halfNewSize = Math.Clamp(
            _dragStartCropArea.Width / 2 * (currentLength / startLength),
            _options.MinimumCropSize / 2,
            CalculateMaximumCropSize() / 2
        );

        halfNewSize = Math.Min(halfNewSize, Math.Min(
            centerX - _imageDisplayOffset.X,
            _imageDisplayOffset.X + _imageDisplaySize.Width - centerX));
        halfNewSize = Math.Min(halfNewSize, Math.Min(
            centerY - _imageDisplayOffset.Y,
            _imageDisplayOffset.Y + _imageDisplaySize.Height - centerY));
        halfNewSize = Math.Max(halfNewSize, _options.MinimumCropSize / 2);

        _cropArea = new Rect(centerX - halfNewSize, centerY - halfNewSize, halfNewSize * 2, halfNewSize * 2);
    }

    private void PerformRotation(Point currentPosition)
    {
        var currentAngle = CalculateAngle(_cropArea.Center, currentPosition);
        _rotationAngle = NormalizeAngle(_rotationInitialAngle + (currentAngle - _rotationStartAngle));
        UpdateRotation();
        UpdateCutout();
    }

    private void UpdateCursor(Point position, Control control)
    {
        var center = new Point(control.Bounds.Width / 2, control.Bounds.Height / 2);
        var radius = control.Bounds.Width / 2;
        var (distance, deltaX, deltaY) = CalculateDistanceAndDeltas(position, center);

        if (IsOnResizeEdge(distance, radius))
            control.Cursor = new Cursor(GetResizeCursor(deltaX, deltaY));
        else if (distance <= radius - 20)
            control.Cursor = new Cursor(StandardCursorType.SizeAll);
        else
            control.Cursor = Cursor.Default;
    }

    private void NotifyCropChanged()
    {
        var now = DateTime.Now;
        if ((now - _lastPreviewUpdate).TotalMilliseconds >= _options.PreviewThrottleMs)
        {
            _lastPreviewUpdate = now;
            CropChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion

    #region Image Processing

    private Bitmap? CreateCroppedImageInternal(int outputSize)
    {
        if (!IsCropStateValid()) return null;

        var sourceBitmap = _fullResolutionBitmap ?? _currentBitmap;
        if (sourceBitmap == null) return null;

        var scaleX = sourceBitmap.PixelSize.Width / _imageDisplaySize.Width;
        var scaleY = sourceBitmap.PixelSize.Height / _imageDisplaySize.Height;
        var cropX = (_cropArea.X - _imageDisplayOffset.X) * scaleX;
        var cropY = (_cropArea.Y - _imageDisplayOffset.Y) * scaleY;
        var cropWidth = _cropArea.Width * scaleX;
        var cropHeight = _cropArea.Height * scaleY;

        var renderTarget = new RenderTargetBitmap(new PixelSize(outputSize, outputSize));
        using var ctx = renderTarget.CreateDrawingContext();

        var clipRect = new Rect(0, 0, outputSize, outputSize);

        Geometry clipGeometry = _options.CropShape == CropShape.Circle
            ? new EllipseGeometry(clipRect)
            : new RectangleGeometry(clipRect);

        using (ctx.PushClip(clipRect))
        using (ctx.PushGeometryClip(clipGeometry))
        {
            if (Math.Abs(_rotationAngle) <= 0.01)
            {
                var sourceRect = new Rect(cropX, cropY, cropWidth, cropHeight);
                var destRect = new Rect(0, 0, outputSize, outputSize);
                ctx.DrawImage(sourceBitmap, sourceRect, destRect);
            }
            else
            {
                var centerX = cropX + cropWidth / 2.0;
                var centerY = cropY + cropHeight / 2.0;
                var scale = outputSize / Math.Max(cropWidth, cropHeight);

                using (ctx.PushTransform(
                    Matrix.CreateTranslation(-centerX, -centerY) *
                    Matrix.CreateRotation(_rotationAngle * Math.PI / 180) *
                    Matrix.CreateScale(scale, scale) *
                    Matrix.CreateTranslation(outputSize / 2.0, outputSize / 2.0)))
                {
                    ctx.DrawImage(sourceBitmap,
                        new Rect(0, 0, sourceBitmap.PixelSize.Width, sourceBitmap.PixelSize.Height),
                        new Rect(0, 0, sourceBitmap.PixelSize.Width, sourceBitmap.PixelSize.Height));
                }
            }
        }

        return renderTarget;
    }

    private static Bitmap RotateBitmap(Bitmap source, int degrees)
    {
        if (degrees % 360 == 0) return source;

        int normalizedDegrees = ((degrees % 360) + 360) % 360;
        int newWidth, newHeight;

        if (normalizedDegrees == 90 || normalizedDegrees == 270)
        {
            newWidth = source.PixelSize.Height;
            newHeight = source.PixelSize.Width;
        }
        else
        {
            newWidth = source.PixelSize.Width;
            newHeight = source.PixelSize.Height;
        }

        var rotated = new RenderTargetBitmap(new PixelSize(newWidth, newHeight));
        using var context = rotated.CreateDrawingContext();

        var matrix = normalizedDegrees switch
        {
            90 => Matrix.CreateTranslation(-source.PixelSize.Width / 2.0, -source.PixelSize.Height / 2.0) *
                  Matrix.CreateRotation(Math.PI / 2) *
                  Matrix.CreateTranslation(newWidth / 2.0, newHeight / 2.0),
            180 => Matrix.CreateTranslation(-source.PixelSize.Width / 2.0, -source.PixelSize.Height / 2.0) *
                   Matrix.CreateRotation(Math.PI) *
                   Matrix.CreateTranslation(newWidth / 2.0, newHeight / 2.0),
            270 => Matrix.CreateTranslation(-source.PixelSize.Width / 2.0, -source.PixelSize.Height / 2.0) *
                   Matrix.CreateRotation(3 * Math.PI / 2) *
                   Matrix.CreateTranslation(newWidth / 2.0, newHeight / 2.0),
            _ => Matrix.Identity
        };

        using (context.PushTransform(matrix))
        {
            context.DrawImage(source,
                new Rect(0, 0, source.PixelSize.Width, source.PixelSize.Height),
                new Rect(0, 0, source.PixelSize.Width, source.PixelSize.Height));
        }

        source.Dispose();
        return rotated;
    }

    #endregion

    #region Utility Methods

    private bool IsCropStateValid() =>
        _currentBitmap != null &&
        _cropArea.Width > 0 &&
        _cropArea.Height > 0 &&
        _imageDisplaySize.Width > 0 &&
        _imageDisplaySize.Height > 0;

    private void NormalizeRotationAngle()
    {
        while (_rotationAngle <= -180) _rotationAngle += 360;
        while (_rotationAngle > 180) _rotationAngle -= 360;
    }

    private static double NormalizeAngle(double angle)
    {
        while (angle <= -180) angle += 360;
        while (angle > 180) angle -= 360;
        return angle;
    }

    private static bool IsLeftButtonPressed(PointerPressedEventArgs e, Control control) =>
        e.GetCurrentPoint(control).Properties.IsLeftButtonPressed;

    private static bool ContinueDragOperation(PointerEventArgs e, Control control) =>
        e.GetCurrentPoint(control).Properties.IsLeftButtonPressed;

    private static (double distance, double deltaX, double deltaY) CalculateDistanceAndDeltas(Point position, Point center)
    {
        var deltaX = position.X - center.X;
        var deltaY = position.Y - center.Y;
        var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        return (distance, deltaX, deltaY);
    }

    private static bool IsOnResizeEdge(double distance, double radius) =>
        distance > radius - 20 && distance < radius + 10;

    private static double CalculateAngle(Point center, Point point) =>
        Math.Atan2(point.Y - center.Y, point.X - center.X) * 180 / Math.PI;

    private static Rect SnapToPixels(Rect rect)
    {
        var left = Math.Floor(rect.X);
        var top = Math.Floor(rect.Y);
        var right = Math.Ceiling(rect.Right);
        var bottom = Math.Ceiling(rect.Bottom);
        return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    private static StandardCursorType GetResizeCursor(double deltaX, double deltaY)
    {
        var angle = Math.Atan2(deltaY, deltaX) * 180 / Math.PI;
        if (angle < 0) angle += 360;
        bool isHorizontal = (angle >= 315 || angle < 45) || (angle >= 135 && angle < 225);
        return isHorizontal ? StandardCursorType.SizeWestEast : StandardCursorType.SizeNorthSouth;
    }

    #endregion
}
