using Avalonia.Media.Imaging;

namespace Avalonia.ImageCropper;

/// <summary>
/// Represents the result of a crop operation.
/// </summary>
public class CropResult
{
    /// <summary>
    /// The path where the cropped image was saved.
    /// </summary>
    public string? SavedPath { get; set; }

    /// <summary>
    /// The path to the original (uncropped) image.
    /// </summary>
    public string? OriginalImagePath { get; set; }

    /// <summary>
    /// The crop settings used, serialized as JSON.
    /// </summary>
    public string? CropSettingsJson { get; set; }

    /// <summary>
    /// The cropped bitmap (only available before disposal).
    /// </summary>
    public Bitmap? CroppedBitmap { get; set; }
}

/// <summary>
/// Represents the crop area settings that can be serialized and restored.
/// </summary>
public class CropSettings
{
    /// <summary>
    /// The X position of the crop area.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// The Y position of the crop area.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// The width of the crop area.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// The height of the crop area.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// The rotation angle in degrees.
    /// </summary>
    public double RotationAngle { get; set; }

    /// <summary>
    /// The display width of the image when settings were saved.
    /// </summary>
    public double ImageDisplayWidth { get; set; }

    /// <summary>
    /// The display height of the image when settings were saved.
    /// </summary>
    public double ImageDisplayHeight { get; set; }

    /// <summary>
    /// The X offset of the image display.
    /// </summary>
    public double ImageDisplayOffsetX { get; set; }

    /// <summary>
    /// The Y offset of the image display.
    /// </summary>
    public double ImageDisplayOffsetY { get; set; }

    /// <summary>
    /// The zoom/scale level of the image.
    /// </summary>
    public double Scale { get; set; } = 1.0;
}
