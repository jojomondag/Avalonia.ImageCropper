using Avalonia.ImageCropper;
using Xunit;

namespace Avalonia.ImageCropper.Tests.Models;

public class CropSettingsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultScale()
    {
        var settings = new CropSettings();

        Assert.Equal(1.0, settings.Scale);
    }

    [Fact]
    public void Constructor_ShouldInitializeNumericPropertiesWithZero()
    {
        var settings = new CropSettings();

        Assert.Equal(0, settings.X);
        Assert.Equal(0, settings.Y);
        Assert.Equal(0, settings.Width);
        Assert.Equal(0, settings.Height);
        Assert.Equal(0, settings.RotationAngle);
        Assert.Equal(0, settings.ImageDisplayWidth);
        Assert.Equal(0, settings.ImageDisplayHeight);
        Assert.Equal(0, settings.ImageDisplayOffsetX);
        Assert.Equal(0, settings.ImageDisplayOffsetY);
    }

    [Fact]
    public void Position_ShouldSetAndGetCorrectly()
    {
        var settings = new CropSettings
        {
            X = 100.5,
            Y = 200.75
        };

        Assert.Equal(100.5, settings.X);
        Assert.Equal(200.75, settings.Y);
    }

    [Fact]
    public void Size_ShouldSetAndGetCorrectly()
    {
        var settings = new CropSettings
        {
            Width = 300.0,
            Height = 400.0
        };

        Assert.Equal(300.0, settings.Width);
        Assert.Equal(400.0, settings.Height);
    }

    [Fact]
    public void RotationAngle_ShouldSetAndGetCorrectly()
    {
        var settings = new CropSettings();

        settings.RotationAngle = 45.0;
        Assert.Equal(45.0, settings.RotationAngle);

        settings.RotationAngle = -90.0;
        Assert.Equal(-90.0, settings.RotationAngle);

        settings.RotationAngle = 360.0;
        Assert.Equal(360.0, settings.RotationAngle);
    }

    [Fact]
    public void Scale_ShouldSetAndGetCorrectly()
    {
        var settings = new CropSettings();

        settings.Scale = 2.5;
        Assert.Equal(2.5, settings.Scale);

        settings.Scale = 0.5;
        Assert.Equal(0.5, settings.Scale);
    }

    [Fact]
    public void ImageDisplayDimensions_ShouldSetAndGetCorrectly()
    {
        var settings = new CropSettings
        {
            ImageDisplayWidth = 1920.0,
            ImageDisplayHeight = 1080.0,
            ImageDisplayOffsetX = 50.0,
            ImageDisplayOffsetY = 25.0
        };

        Assert.Equal(1920.0, settings.ImageDisplayWidth);
        Assert.Equal(1080.0, settings.ImageDisplayHeight);
        Assert.Equal(50.0, settings.ImageDisplayOffsetX);
        Assert.Equal(25.0, settings.ImageDisplayOffsetY);
    }

    [Fact]
    public void AllProperties_ShouldBeSettableToArbitraryValues()
    {
        var settings = new CropSettings
        {
            X = 123.456,
            Y = 789.012,
            Width = 500.5,
            Height = 600.6,
            RotationAngle = 180.0,
            ImageDisplayWidth = 2560.0,
            ImageDisplayHeight = 1440.0,
            ImageDisplayOffsetX = 100.0,
            ImageDisplayOffsetY = 50.0,
            Scale = 1.75
        };

        Assert.Equal(123.456, settings.X);
        Assert.Equal(789.012, settings.Y);
        Assert.Equal(500.5, settings.Width);
        Assert.Equal(600.6, settings.Height);
        Assert.Equal(180.0, settings.RotationAngle);
        Assert.Equal(2560.0, settings.ImageDisplayWidth);
        Assert.Equal(1440.0, settings.ImageDisplayHeight);
        Assert.Equal(100.0, settings.ImageDisplayOffsetX);
        Assert.Equal(50.0, settings.ImageDisplayOffsetY);
        Assert.Equal(1.75, settings.Scale);
    }
}
