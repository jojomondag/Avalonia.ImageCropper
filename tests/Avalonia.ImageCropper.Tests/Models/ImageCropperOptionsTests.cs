using Avalonia.ImageCropper;
using Xunit;

namespace Avalonia.ImageCropper.Tests.Models;

public class ImageCropperOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeOutputSizeToDefault()
    {
        var options = new ImageCropperOptions();

        Assert.Equal(512, options.OutputSize);
    }

    [Fact]
    public void Constructor_ShouldInitializeCropShapeToCircle()
    {
        var options = new ImageCropperOptions();

        Assert.Equal(CropShape.Circle, options.CropShape);
    }

    [Fact]
    public void Constructor_ShouldInitializeMinimumCropSizeToDefault()
    {
        var options = new ImageCropperOptions();

        Assert.Equal(50, options.MinimumCropSize);
    }

    [Fact]
    public void Constructor_ShouldInitializeMaximumCropSizeToDefault()
    {
        var options = new ImageCropperOptions();

        Assert.Equal(2000, options.MaximumCropSize);
    }

    [Fact]
    public void Constructor_ShouldInitializeMaximumCropRatioToDefault()
    {
        var options = new ImageCropperOptions();

        Assert.Equal(0.95, options.MaximumCropRatio);
    }

    [Fact]
    public void Constructor_ShouldInitializeMaximumDisplayImageSizeToDefault()
    {
        var options = new ImageCropperOptions();

        Assert.Equal(4096, options.MaximumDisplayImageSize);
    }

    [Fact]
    public void Constructor_ShouldInitializeShowImageGalleryToTrue()
    {
        var options = new ImageCropperOptions();

        Assert.True(options.ShowImageGallery);
    }

    [Fact]
    public void Constructor_ShouldInitializeAllowRotationToTrue()
    {
        var options = new ImageCropperOptions();

        Assert.True(options.AllowRotation);
    }

    [Fact]
    public void Constructor_ShouldInitializePreviewThrottleMsToDefault()
    {
        var options = new ImageCropperOptions();

        Assert.Equal(33, options.PreviewThrottleMs);
    }

    [Fact]
    public void Constructor_ShouldInitializeDirectoriesToNull()
    {
        var options = new ImageCropperOptions();

        Assert.Null(options.OriginalsDirectory);
        Assert.Null(options.OutputDirectory);
    }

    [Fact]
    public void SupportedImageExtensions_ShouldContainExpectedFormats()
    {
        var options = new ImageCropperOptions();

        Assert.Contains(".jpg", options.SupportedImageExtensions);
        Assert.Contains(".jpeg", options.SupportedImageExtensions);
        Assert.Contains(".png", options.SupportedImageExtensions);
        Assert.Contains(".bmp", options.SupportedImageExtensions);
        Assert.Contains(".gif", options.SupportedImageExtensions);
        Assert.Contains(".webp", options.SupportedImageExtensions);
    }

    [Fact]
    public void SupportedImageExtensions_ShouldHaveCorrectCount()
    {
        var options = new ImageCropperOptions();

        Assert.Equal(6, options.SupportedImageExtensions.Length);
    }

    [Fact]
    public void OutputSize_ShouldBeSettable()
    {
        var options = new ImageCropperOptions();

        options.OutputSize = 1024;

        Assert.Equal(1024, options.OutputSize);
    }

    [Fact]
    public void CropShape_ShouldBeSettableToSquare()
    {
        var options = new ImageCropperOptions();

        options.CropShape = CropShape.Square;

        Assert.Equal(CropShape.Square, options.CropShape);
    }

    [Fact]
    public void ShowImageGallery_ShouldBeSettable()
    {
        var options = new ImageCropperOptions();

        options.ShowImageGallery = false;

        Assert.False(options.ShowImageGallery);
    }

    [Fact]
    public void AllowRotation_ShouldBeSettable()
    {
        var options = new ImageCropperOptions();

        options.AllowRotation = false;

        Assert.False(options.AllowRotation);
    }

    [Fact]
    public void SupportedImageExtensions_ShouldBeSettable()
    {
        var options = new ImageCropperOptions();
        var customExtensions = new[] { ".tiff", ".svg" };

        options.SupportedImageExtensions = customExtensions;

        Assert.Equal(customExtensions, options.SupportedImageExtensions);
        Assert.Equal(2, options.SupportedImageExtensions.Length);
    }

    [Fact]
    public void Directories_ShouldBeSettable()
    {
        var options = new ImageCropperOptions
        {
            OriginalsDirectory = "/path/to/originals",
            OutputDirectory = "/path/to/output"
        };

        Assert.Equal("/path/to/originals", options.OriginalsDirectory);
        Assert.Equal("/path/to/output", options.OutputDirectory);
    }
}
