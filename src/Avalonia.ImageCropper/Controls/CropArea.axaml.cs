using Avalonia;
using Avalonia.Controls;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Avalonia.ImageCropper.Controls;

/// <summary>
/// The main crop area control that handles image display and crop selection.
/// Instagram-style: fixed crop circle, pan/zoom/rotate the image underneath.
/// Supports multi-touch gestures: pinch-to-zoom and two-finger rotation.
/// </summary>
public partial class CropArea : UserControl
{
    #region Fields

    private Bitmap? _currentBitmap;
    private Bitmap? _fullResolutionBitmap;
    private bool _isDragging;
    private Point _pointerStartPosition;
    private Point _imageOffset;
    private Point _dragStartOffset;
    private double _imageScale = 1.0;
    private double _imageRotation; // Continuous rotation in degrees
    private double _cropCircleSize;
    private ImageCropperOptions _options = new();
    private DateTime _lastPreviewUpdate = DateTime.MinValue;

    // Multi-touch gesture tracking
    private readonly Dictionary<int, Point> _activePointers = new();
    private double _gestureStartDistance;
    private double _gestureStartAngle;
    private double _gestureStartScale;
    private double _gestureStartRotation;
    private Point _gestureStartOffset;
    private bool _isMultiTouchGesture;

    // Border rotation
    private bool _isRotating;
    private double _rotateStartAngle;
    private double _rotateStartRotation;
    private bool _isHoveringBorder;

    private Grid? _backgroundPattern;
    private Image? _mainImage;
    private Grid? _cropOverlay;
    private AvaloniaPath? _overlayCutout;
    private Border? _cropCircle;
    private Border? _interactionLayer;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="CropArea"/> class.
    /// </summary>
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
        _cropCircle = this.FindControl<Border>("CropCircle");
        _interactionLayer = this.FindControl<Border>("InteractionLayer");

        var mainArea = this.FindControl<Border>("MainImageArea");
        if (mainArea != null)
        {
            mainArea.PointerPressed += (s, e) =>
            {
                if (_currentBitmap == null)
                {
                    ImageAreaClicked?.Invoke(this, EventArgs.Empty);
                }
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
        UpdateCropCircle();
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
    /// Rotates the image by the specified degrees (adds to current rotation).
    /// </summary>
    public void Rotate(double degrees)
    {
        if (_currentBitmap == null) return;

        _imageRotation += degrees;

        // Normalize to -180 to 180
        while (_imageRotation > 180) _imageRotation -= 360;
        while (_imageRotation < -180) _imageRotation += 360;

        ApplyImageTransform();
        CropChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Resets the crop area to default.
    /// </summary>
    public void ResetCrop()
    {
        if (_currentBitmap != null)
        {
            ResetImageTransform();
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
        _imageOffset = default;
        _imageScale = 1.0;
        _imageRotation = 0;
        _activePointers.Clear();
        _isDragging = false;
        _isMultiTouchGesture = false;

        if (_mainImage != null) _mainImage.IsVisible = false;
        if (_backgroundPattern != null) _backgroundPattern.IsVisible = true;
        if (_cropOverlay != null) _cropOverlay.IsVisible = false;
        if (_interactionLayer != null) _interactionLayer.IsVisible = false;
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
        if (_currentBitmap == null || _cropOverlay == null) return null;

        var containerBounds = _cropOverlay.Bounds;
        var cropRect = GetCropRectInImageCoordinates();

        return new CropSettings
        {
            X = cropRect.X,
            Y = cropRect.Y,
            Width = cropRect.Width,
            Height = cropRect.Height,
            RotationAngle = _imageRotation,
            ImageDisplayWidth = containerBounds.Width,
            ImageDisplayHeight = containerBounds.Height,
            ImageDisplayOffsetX = _imageOffset.X,
            ImageDisplayOffsetY = _imageOffset.Y,
            Scale = _imageScale
        };
    }

    /// <summary>
    /// Gets the current rotation angle in degrees (-180 to 180).
    /// </summary>
    public double CurrentRotation => _imageRotation;

    /// <summary>
    /// Gets the current zoom/scale level.
    /// </summary>
    public double CurrentScale => _imageScale;

    /// <summary>
    /// Sets the rotation angle in degrees. The image rotates around its center.
    /// </summary>
    /// <param name="degrees">Rotation angle in degrees</param>
    public void SetRotation(double degrees)
    {
        if (_currentBitmap == null) return;

        _imageRotation = degrees;

        // Normalize to -180 to 180
        while (_imageRotation > 180) _imageRotation -= 360;
        while (_imageRotation < -180) _imageRotation += 360;

        ApplyImageTransform();
        CropChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets the zoom/scale level.
    /// </summary>
    /// <param name="scale">Scale value (1.0 = 100%)</param>
    public void SetScale(double scale)
    {
        if (_currentBitmap == null) return;

        // Very permissive limits
        _imageScale = Math.Clamp(scale, 0.1, 10.0);

        ApplyImageTransform();
        CropChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Image Loading

    private async Task LoadBitmapAsync(Bitmap bitmap, CropSettings? settings = null)
    {
        _currentBitmap?.Dispose();
        _currentBitmap = bitmap;

        if (_mainImage == null || _backgroundPattern == null || _cropOverlay == null || _interactionLayer == null) return;

        _mainImage.Source = _currentBitmap;
        _mainImage.IsVisible = true;
        _backgroundPattern.IsVisible = false;
        _cropOverlay.IsVisible = true;
        _interactionLayer.IsVisible = true;

        await Task.Delay(50);

        UpdateCropCircle();
        RestoreOrResetTransform(settings);
        CropChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RestoreOrResetTransform(CropSettings? settings)
    {
        if (settings != null && _currentBitmap != null)
        {
            _imageRotation = settings.RotationAngle;
            _imageScale = settings.Scale > 0 ? settings.Scale : 1.0;
            _imageOffset = new Point(settings.ImageDisplayOffsetX, settings.ImageDisplayOffsetY);
            ApplyImageTransform();
        }
        else
        {
            ResetImageTransform();
        }
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
        // Note: Avalonia's Bitmap loader handles EXIF orientation automatically
        return new Bitmap(stream);
    }

    #endregion

    #region Crop Circle Management

    private void UpdateCropCircle()
    {
        if (_cropOverlay == null || _cropCircle == null) return;

        var containerBounds = _cropOverlay.Bounds;
        if (containerBounds.Width == 0 || containerBounds.Height == 0) return;

        // Crop circle size: 70% of the smaller dimension
        _cropCircleSize = Math.Min(containerBounds.Width, containerBounds.Height) * 0.7;
        _cropCircleSize = Math.Clamp(_cropCircleSize, _options.MinimumCropSize, _options.MaximumCropSize);

        // Update crop circle border
        _cropCircle.Width = _cropCircleSize;
        _cropCircle.Height = _cropCircleSize;
        _cropCircle.CornerRadius = new CornerRadius(_cropCircleSize / 2);

        UpdateCutout();
    }

    private void UpdateCutout()
    {
        if (_cropOverlay == null || _overlayCutout == null) return;

        var containerBounds = _cropOverlay.Bounds;
        if (containerBounds.Width == 0 || containerBounds.Height == 0) return;

        var outerRect = new Rect(0, 0, containerBounds.Width, containerBounds.Height);
        var center = new Point(containerBounds.Width / 2, containerBounds.Height / 2);
        var radius = _cropCircleSize / 2;

        Geometry innerGeometry = _options.CropShape == CropShape.Circle
            ? new EllipseGeometry { Center = center, RadiusX = radius, RadiusY = radius }
            : new RectangleGeometry(new Rect(center.X - radius, center.Y - radius, _cropCircleSize, _cropCircleSize));

        _overlayCutout.Data = new GeometryGroup
        {
            FillRule = FillRule.EvenOdd,
            Children = { new RectangleGeometry(outerRect), innerGeometry }
        };
    }

    private void ResetImageTransform()
    {
        if (_currentBitmap == null || _cropOverlay == null || _mainImage == null) return;

        var containerBounds = _cropOverlay.Bounds;
        if (containerBounds.Width == 0 || containerBounds.Height == 0) return;

        // Calculate scale to fit image so crop circle is filled
        var imageWidth = _currentBitmap.PixelSize.Width;
        var imageHeight = _currentBitmap.PixelSize.Height;

        // Scale so the smaller image dimension matches the crop circle
        var scaleToFillCrop = _cropCircleSize / Math.Min(imageWidth, imageHeight);
        _imageScale = scaleToFillCrop;

        // Reset rotation and offset
        _imageRotation = 0;
        _imageOffset = new Point(0, 0);

        ApplyImageTransform();
    }

    private void ApplyImageTransform()
    {
        if (_mainImage == null || _currentBitmap == null || _cropOverlay == null) return;

        // Image dimensions
        var imageWidth = _currentBitmap.PixelSize.Width;
        var imageHeight = _currentBitmap.PixelSize.Height;
        var imageCenterX = imageWidth / 2.0;
        var imageCenterY = imageHeight / 2.0;

        // Container dimensions - where the crop circle is centered
        var containerBounds = _cropOverlay.Bounds;
        var containerCenterX = containerBounds.Width / 2.0;
        var containerCenterY = containerBounds.Height / 2.0;

        var rotRad = _imageRotation * Math.PI / 180;

        // Build transform to:
        // 1. Move image center to origin
        // 2. Scale
        // 3. Apply pan offset
        // 4. Rotate
        // 5. Move to container center (where crop circle is)
        var matrix =
            Matrix.CreateTranslation(-imageCenterX, -imageCenterY) *
            Matrix.CreateScale(_imageScale, _imageScale) *
            Matrix.CreateTranslation(_imageOffset.X, _imageOffset.Y) *
            Matrix.CreateRotation(rotRad) *
            Matrix.CreateTranslation(containerCenterX, containerCenterY);

        _mainImage.RenderTransform = new MatrixTransform(matrix);
    }

    private Rect GetCropRectInImageCoordinates()
    {
        if (_currentBitmap == null || _cropOverlay == null) return default;

        var containerBounds = _cropOverlay.Bounds;
        var containerCenterX = containerBounds.Width / 2;
        var containerCenterY = containerBounds.Height / 2;

        var imageWidth = _currentBitmap.PixelSize.Width;
        var imageHeight = _currentBitmap.PixelSize.Height;

        // The crop center relative to image center (accounting for offset)
        // Offset moves the image, so to find where crop is relative to image, we invert
        var cropRelativeX = -_imageOffset.X;
        var cropRelativeY = -_imageOffset.Y;

        // Apply inverse rotation to get position in unrotated image space
        var rotationRad = -_imageRotation * Math.PI / 180;
        var cos = Math.Cos(rotationRad);
        var sin = Math.Sin(rotationRad);

        var unrotatedX = cropRelativeX * cos - cropRelativeY * sin;
        var unrotatedY = cropRelativeX * sin + cropRelativeY * cos;

        // Convert from scaled coordinates to original image coordinates
        var cropInImageX = (imageWidth / 2) + (unrotatedX / _imageScale) - (_cropCircleSize / _imageScale / 2);
        var cropInImageY = (imageHeight / 2) + (unrotatedY / _imageScale) - (_cropCircleSize / _imageScale / 2);
        var cropInImageSize = _cropCircleSize / _imageScale;

        return new Rect(cropInImageX, cropInImageY, cropInImageSize, cropInImageSize);
    }

    #endregion

    #region Event Handlers

    private void InitializeEventHandlers()
    {
        if (_interactionLayer == null || _cropOverlay == null) return;

        _interactionLayer.PointerPressed += OnInteractionPressed;
        _interactionLayer.PointerMoved += OnInteractionMoved;
        _interactionLayer.PointerReleased += OnInteractionReleased;
        _interactionLayer.PointerCaptureLost += OnPointerCaptureLost;
        _interactionLayer.PointerWheelChanged += OnPointerWheelChanged;
        _interactionLayer.PointerExited += OnPointerExited;

        _cropOverlay.SizeChanged += (s, e) =>
        {
            UpdateCropCircle();
            if (_currentBitmap != null)
            {
                ResetImageTransform();
            }
        };
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (!_isRotating && !_isDragging)
        {
            SetBorderHover(false);
        }
    }

    private bool IsNearBorder(Point position)
    {
        if (_cropOverlay == null) return false;

        var containerBounds = _cropOverlay.Bounds;
        var center = new Point(containerBounds.Width / 2, containerBounds.Height / 2);
        var radius = _cropCircleSize / 2;

        // Distance from center
        var dx = position.X - center.X;
        var dy = position.Y - center.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        // Near border if within 20 pixels of the circle edge
        var borderThickness = 20;
        return Math.Abs(distance - radius) < borderThickness;
    }

    private void SetBorderHover(bool isHovering)
    {
        if (_isHoveringBorder == isHovering) return;
        _isHoveringBorder = isHovering;

        if (_cropCircle != null)
        {
            if (isHovering)
            {
                _cropCircle.BorderBrush = new SolidColorBrush(Color.FromRgb(100, 200, 255));
                _cropCircle.BorderThickness = new Thickness(3);
                _cropCircle.BoxShadow = new BoxShadows(new BoxShadow
                {
                    Color = Color.FromArgb(150, 100, 200, 255),
                    Blur = 10,
                    Spread = 2
                });
            }
            else
            {
                _cropCircle.BorderBrush = new SolidColorBrush(Colors.White);
                _cropCircle.BorderThickness = new Thickness(2);
                _cropCircle.BoxShadow = new BoxShadows(new BoxShadow
                {
                    Color = Color.FromArgb(64, 0, 0, 0),
                    Blur = 0,
                    Spread = 1
                });
            }
        }

        if (_interactionLayer != null && !_isDragging && !_isRotating)
        {
            _interactionLayer.Cursor = isHovering
                ? new Cursor(StandardCursorType.Cross)
                : new Cursor(StandardCursorType.Hand);
        }
    }

    private void OnInteractionPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_interactionLayer == null || _currentBitmap == null) return;

        var point = e.GetCurrentPoint(_interactionLayer);
        var position = e.GetPosition(_interactionLayer);
        var pointerId = (int)e.Pointer.Id;

        e.Pointer.Capture(_interactionLayer);

        // Left-click on border = rotate, elsewhere = pan
        if (point.Properties.IsLeftButtonPressed)
        {
            _activePointers[pointerId] = position;

            // Check if clicking on/near the border
            if (IsNearBorder(position) && _activePointers.Count == 1)
            {
                // Start rotation
                _isRotating = true;
                _isDragging = false;
                _isMultiTouchGesture = false;

                var containerBounds = _cropOverlay?.Bounds ?? default;
                var center = new Point(containerBounds.Width / 2, containerBounds.Height / 2);
                _rotateStartAngle = Math.Atan2(position.Y - center.Y, position.X - center.X) * 180 / Math.PI;
                _rotateStartRotation = _imageRotation;
                _interactionLayer.Cursor = new Cursor(StandardCursorType.Cross);
            }
            else if (_activePointers.Count == 1)
            {
                // Start pan
                _isDragging = true;
                _isMultiTouchGesture = false;
                _isRotating = false;
                _pointerStartPosition = position;
                _dragStartOffset = _imageOffset;
                _interactionLayer.Cursor = new Cursor(StandardCursorType.SizeAll);
            }
            else if (_activePointers.Count == 2)
            {
                // Two fingers - start pinch/rotate gesture
                _isDragging = false;
                _isMultiTouchGesture = true;
                _isRotating = false;
                InitializeMultiTouchGesture();
            }

            e.Handled = true;
        }
    }

    private void OnInteractionMoved(object? sender, PointerEventArgs e)
    {
        if (_interactionLayer == null) return;

        var position = e.GetPosition(_interactionLayer);
        var pointerId = (int)e.Pointer.Id;

        // Update hover state when not actively dragging/rotating
        if (!_isDragging && !_isRotating && !_isMultiTouchGesture)
        {
            SetBorderHover(IsNearBorder(position));
        }

        if (_currentBitmap == null) return;

        // Handle border rotation
        if (_isRotating)
        {
            var containerBounds = _cropOverlay?.Bounds ?? default;
            var center = new Point(containerBounds.Width / 2, containerBounds.Height / 2);
            var currentAngle = Math.Atan2(position.Y - center.Y, position.X - center.X) * 180 / Math.PI;
            var angleDelta = currentAngle - _rotateStartAngle;

            _imageRotation = _rotateStartRotation + angleDelta;

            // Normalize rotation
            while (_imageRotation > 180) _imageRotation -= 360;
            while (_imageRotation < -180) _imageRotation += 360;

            ApplyImageTransform();
            NotifyCropChanged();
            e.Handled = true;
            return;
        }

        if (!_activePointers.ContainsKey(pointerId)) return;
        _activePointers[pointerId] = position;

        if (_isMultiTouchGesture && _activePointers.Count >= 2)
        {
            // Handle pinch-to-zoom and rotation
            HandleMultiTouchGesture();
        }
        else if (_isDragging && _activePointers.Count == 1)
        {
            // Handle pan - FREE movement, no constraints
            var delta = position - _pointerStartPosition;

            _imageOffset = new Point(
                _dragStartOffset.X + delta.X,
                _dragStartOffset.Y + delta.Y
            );

            ApplyImageTransform();
            NotifyCropChanged();
        }

        e.Handled = true;
    }

    private void OnInteractionReleased(object? sender, PointerReleasedEventArgs e)
    {
        var pointerId = (int)e.Pointer.Id;
        var position = e.GetPosition(_interactionLayer);
        _activePointers.Remove(pointerId);
        e.Pointer.Capture(null);

        // End rotation
        if (_isRotating)
        {
            _isRotating = false;
            SetBorderHover(IsNearBorder(position));
            CropChanged?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }

        if (_activePointers.Count == 0)
        {
            // All fingers lifted
            _isDragging = false;
            _isMultiTouchGesture = false;
            SetBorderHover(IsNearBorder(position));
            CropChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (_activePointers.Count == 1 && _isMultiTouchGesture)
        {
            // Went from 2 fingers to 1 - switch to pan mode
            _isMultiTouchGesture = false;
            _isDragging = true;
            var remainingPointer = _activePointers.First();
            _pointerStartPosition = remainingPointer.Value;
            _dragStartOffset = _imageOffset;
        }

        e.Handled = true;
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        var pointerId = (int)e.Pointer.Id;
        _activePointers.Remove(pointerId);

        if (_activePointers.Count == 0)
        {
            _isDragging = false;
            _isMultiTouchGesture = false;
        }
    }

    private void InitializeMultiTouchGesture()
    {
        if (_activePointers.Count < 2) return;

        var points = _activePointers.Values.ToList();
        var p1 = points[0];
        var p2 = points[1];

        _gestureStartDistance = GetDistance(p1, p2);
        _gestureStartAngle = GetAngle(p1, p2);
        _gestureStartScale = _imageScale;
        _gestureStartRotation = _imageRotation;
        _gestureStartOffset = _imageOffset;
    }

    private void HandleMultiTouchGesture()
    {
        if (_activePointers.Count < 2 || _currentBitmap == null) return;

        var points = _activePointers.Values.ToList();
        var p1 = points[0];
        var p2 = points[1];

        var currentDistance = GetDistance(p1, p2);
        var currentAngle = GetAngle(p1, p2);

        // Calculate scale change (pinch-to-zoom) - with generous limits
        if (_gestureStartDistance > 0)
        {
            var scaleRatio = currentDistance / _gestureStartDistance;
            var newScale = _gestureStartScale * scaleRatio;

            // Very permissive limits: 0.1x to 10x
            var minScale = 0.1;
            var maxScale = 10.0;

            _imageScale = Math.Clamp(newScale, minScale, maxScale);
        }

        // Calculate rotation change (two-finger rotation)
        var angleDelta = currentAngle - _gestureStartAngle;
        _imageRotation = _gestureStartRotation + angleDelta;

        // Normalize rotation to -180 to 180
        while (_imageRotation > 180) _imageRotation -= 360;
        while (_imageRotation < -180) _imageRotation += 360;

        ApplyImageTransform();
        NotifyCropChanged();
    }

    private static double GetDistance(Point p1, Point p2)
    {
        var dx = p2.X - p1.X;
        var dy = p2.Y - p1.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double GetAngle(Point p1, Point p2)
    {
        return Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) * 180 / Math.PI;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_currentBitmap == null || _cropOverlay == null) return;

        var position = e.GetPosition(_interactionLayer);
        var containerBounds = _cropOverlay.Bounds;
        var containerCenterX = containerBounds.Width / 2;
        var containerCenterY = containerBounds.Height / 2;

        // Mouse position relative to container center
        var mouseFromCenterX = position.X - containerCenterX;
        var mouseFromCenterY = position.Y - containerCenterY;

        // Apply inverse rotation to get mouse position in pre-rotation space
        // (offset is applied BEFORE rotation in the transform)
        var rotRad = _imageRotation * Math.PI / 180;
        var cos = Math.Cos(-rotRad);
        var sin = Math.Sin(-rotRad);

        var mouseInOffsetSpaceX = mouseFromCenterX * cos - mouseFromCenterY * sin;
        var mouseInOffsetSpaceY = mouseFromCenterX * sin + mouseFromCenterY * cos;

        // Zoom
        var scaleFactor = e.Delta.Y > 0 ? 1.15 : 0.87;
        var oldScale = _imageScale;
        var newScale = Math.Clamp(_imageScale * scaleFactor, 0.1, 10.0);

        if (Math.Abs(newScale - oldScale) > 0.001)
        {
            var scaleRatio = newScale / oldScale;
            _imageScale = newScale;

            // Adjust offset so the point under the mouse stays under the mouse
            // Formula: newOffset = mousePos * (1 - scaleRatio) + oldOffset * scaleRatio
            _imageOffset = new Point(
                mouseInOffsetSpaceX * (1 - scaleRatio) + _imageOffset.X * scaleRatio,
                mouseInOffsetSpaceY * (1 - scaleRatio) + _imageOffset.Y * scaleRatio
            );

            ApplyImageTransform();
            CropChanged?.Invoke(this, EventArgs.Empty);
        }

        e.Handled = true;
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
        if (_currentBitmap == null || _cropOverlay == null) return null;

        if (_cropCircleSize <= 0) return null;

        var bitmap = _currentBitmap;
        var bitmapWidth = bitmap.PixelSize.Width;
        var bitmapHeight = bitmap.PixelSize.Height;
        var imageCenterX = bitmapWidth / 2.0;
        var imageCenterY = bitmapHeight / 2.0;

        // Container center (where crop circle is)
        var containerBounds = _cropOverlay.Bounds;
        var containerCenterX = containerBounds.Width / 2.0;
        var containerCenterY = containerBounds.Height / 2.0;

        var rotRad = _imageRotation * Math.PI / 180;

        // Build the EXACT same transform as the display uses
        var displayTransform =
            Matrix.CreateTranslation(-imageCenterX, -imageCenterY) *
            Matrix.CreateScale(_imageScale, _imageScale) *
            Matrix.CreateTranslation(_imageOffset.X, _imageOffset.Y) *
            Matrix.CreateRotation(rotRad) *
            Matrix.CreateTranslation(containerCenterX, containerCenterY);

        // The crop circle is at container center (containerCenterX, containerCenterY)
        // To make the preview show the same thing:
        // 1. Apply display transform (bitmap → screen)
        // 2. Translate so crop circle center goes to origin
        // 3. Scale to fit output
        // 4. Translate to output center
        var previewScale = (double)outputSize / _cropCircleSize;

        var previewTransform = displayTransform *
            Matrix.CreateTranslation(-containerCenterX, -containerCenterY) *
            Matrix.CreateScale(previewScale, previewScale) *
            Matrix.CreateTranslation(outputSize / 2.0, outputSize / 2.0);

        var renderTarget = new RenderTargetBitmap(new PixelSize(outputSize, outputSize));
        using var ctx = renderTarget.CreateDrawingContext();

        var clipRect = new Rect(0, 0, outputSize, outputSize);

        Geometry clipGeometry = _options.CropShape == CropShape.Circle
            ? new EllipseGeometry(clipRect)
            : new RectangleGeometry(clipRect);

        using (ctx.PushClip(clipRect))
        using (ctx.PushGeometryClip(clipGeometry))
        using (ctx.PushTransform(previewTransform))
        {
            ctx.DrawImage(bitmap,
                new Rect(0, 0, bitmapWidth, bitmapHeight),
                new Rect(0, 0, bitmapWidth, bitmapHeight));
        }

        return renderTarget;
    }

    #endregion
}
