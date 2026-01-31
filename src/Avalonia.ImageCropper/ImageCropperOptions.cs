namespace Avalonia.ImageCropper;

/// <summary>
/// Configuration options for the ImageCropper control.
/// </summary>
public class ImageCropperOptions
{
    /// <summary>
    /// The output size of the cropped image in pixels. Default is 512.
    /// </summary>
    public int OutputSize { get; set; } = 512;

    /// <summary>
    /// The minimum size of the crop area in pixels. Default is 50.
    /// </summary>
    public double MinimumCropSize { get; set; } = 50;

    /// <summary>
    /// The maximum size of the crop area in pixels. Default is 2000.
    /// </summary>
    public double MaximumCropSize { get; set; } = 2000;

    /// <summary>
    /// The maximum ratio of crop size to image size. Default is 0.95.
    /// </summary>
    public double MaximumCropRatio { get; set; } = 0.95;

    /// <summary>
    /// The maximum size for the display image (for performance). Default is 4096.
    /// </summary>
    public int MaximumDisplayImageSize { get; set; } = 4096;

    /// <summary>
    /// The shape of the crop area. Default is Circle.
    /// </summary>
    public CropShape CropShape { get; set; } = CropShape.Circle;

    /// <summary>
    /// Whether to show the image gallery/history panel. Default is true.
    /// </summary>
    public bool ShowImageGallery { get; set; } = true;

    /// <summary>
    /// Whether to show rotation buttons. Default is true.
    /// </summary>
    public bool AllowRotation { get; set; } = true;

    /// <summary>
    /// The supported image file extensions. Default includes common formats.
    /// </summary>
    public string[] SupportedImageExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };

    /// <summary>
    /// Preview update throttle in milliseconds (for performance). Default is 33ms (30fps).
    /// </summary>
    public int PreviewThrottleMs { get; set; } = 33;

    /// <summary>
    /// The directory to store original images. If null, no gallery is shown.
    /// </summary>
    public string? OriginalsDirectory { get; set; }

    /// <summary>
    /// The default directory to save cropped images.
    /// </summary>
    public string? OutputDirectory { get; set; }
}

/// <summary>
/// The shape of the crop area.
/// </summary>
public enum CropShape
{
    /// <summary>
    /// Circular crop area (for profile pictures/avatars).
    /// </summary>
    Circle,

    /// <summary>
    /// Square crop area.
    /// </summary>
    Square
}
