using System.Text.Json;
using Avalonia.ImageCropper;
using Xunit;

namespace Avalonia.ImageCropper.Tests.Serialization;

public class CropSettingsSerializationTests
{
    [Fact]
    public void Serialize_ShouldProduceValidJson()
    {
        var settings = new CropSettings
        {
            X = 100,
            Y = 200,
            Width = 300,
            Height = 400,
            Scale = 1.5
        };

        var json = JsonSerializer.Serialize(settings);

        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json);
    }

    [Fact]
    public void Deserialize_ShouldRestoreAllProperties()
    {
        var original = new CropSettings
        {
            X = 123.456,
            Y = 789.012,
            Width = 500.5,
            Height = 600.6,
            RotationAngle = 45.0,
            ImageDisplayWidth = 1920.0,
            ImageDisplayHeight = 1080.0,
            ImageDisplayOffsetX = 50.0,
            ImageDisplayOffsetY = 25.0,
            Scale = 2.0
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<CropSettings>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.X, restored.X);
        Assert.Equal(original.Y, restored.Y);
        Assert.Equal(original.Width, restored.Width);
        Assert.Equal(original.Height, restored.Height);
        Assert.Equal(original.RotationAngle, restored.RotationAngle);
        Assert.Equal(original.ImageDisplayWidth, restored.ImageDisplayWidth);
        Assert.Equal(original.ImageDisplayHeight, restored.ImageDisplayHeight);
        Assert.Equal(original.ImageDisplayOffsetX, restored.ImageDisplayOffsetX);
        Assert.Equal(original.ImageDisplayOffsetY, restored.ImageDisplayOffsetY);
        Assert.Equal(original.Scale, restored.Scale);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveDefaultValues()
    {
        var original = new CropSettings();

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<CropSettings>(json);

        Assert.NotNull(restored);
        Assert.Equal(1.0, restored.Scale);
        Assert.Equal(0, restored.X);
        Assert.Equal(0, restored.Y);
    }

    [Fact]
    public void Deserialize_ShouldHandlePartialJson()
    {
        const string partialJson = "{\"X\":100,\"Y\":200}";

        var settings = JsonSerializer.Deserialize<CropSettings>(partialJson);

        Assert.NotNull(settings);
        Assert.Equal(100, settings.X);
        Assert.Equal(200, settings.Y);
        Assert.Equal(0, settings.Width);
        Assert.Equal(0, settings.Height);
    }

    [Fact]
    public void Deserialize_ShouldUsePropertyDefaultScaleWhenNotProvided()
    {
        const string jsonWithoutScale = "{\"X\":50,\"Y\":75}";

        var settings = JsonSerializer.Deserialize<CropSettings>(jsonWithoutScale);

        Assert.NotNull(settings);
        // System.Text.Json in .NET 8+ respects property initializers
        Assert.Equal(1.0, settings.Scale);
    }

    [Fact]
    public void Deserialize_ShouldHandleEmptyJson()
    {
        const string emptyJson = "{}";

        var settings = JsonSerializer.Deserialize<CropSettings>(emptyJson);

        Assert.NotNull(settings);
        Assert.Equal(0, settings.X);
        Assert.Equal(0, settings.Y);
    }

    [Fact]
    public void Serialize_ShouldIncludeAllProperties()
    {
        var settings = new CropSettings
        {
            X = 10,
            Y = 20,
            Width = 100,
            Height = 100,
            RotationAngle = 90,
            Scale = 1.5,
            ImageDisplayWidth = 800,
            ImageDisplayHeight = 600,
            ImageDisplayOffsetX = 5,
            ImageDisplayOffsetY = 10
        };

        var json = JsonSerializer.Serialize(settings);

        Assert.Contains("\"X\"", json);
        Assert.Contains("\"Y\"", json);
        Assert.Contains("\"Width\"", json);
        Assert.Contains("\"Height\"", json);
        Assert.Contains("\"RotationAngle\"", json);
        Assert.Contains("\"Scale\"", json);
        Assert.Contains("\"ImageDisplayWidth\"", json);
        Assert.Contains("\"ImageDisplayHeight\"", json);
        Assert.Contains("\"ImageDisplayOffsetX\"", json);
        Assert.Contains("\"ImageDisplayOffsetY\"", json);
    }

    [Fact]
    public void RoundTrip_WithNegativeValues_ShouldPreserveValues()
    {
        var original = new CropSettings
        {
            X = -50.0,
            Y = -100.0,
            RotationAngle = -45.0,
            ImageDisplayOffsetX = -25.0,
            ImageDisplayOffsetY = -30.0
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<CropSettings>(json);

        Assert.NotNull(restored);
        Assert.Equal(-50.0, restored.X);
        Assert.Equal(-100.0, restored.Y);
        Assert.Equal(-45.0, restored.RotationAngle);
        Assert.Equal(-25.0, restored.ImageDisplayOffsetX);
        Assert.Equal(-30.0, restored.ImageDisplayOffsetY);
    }

    [Fact]
    public void RoundTrip_WithLargeValues_ShouldPreserveValues()
    {
        var original = new CropSettings
        {
            Width = 10000.0,
            Height = 10000.0,
            ImageDisplayWidth = 8192.0,
            ImageDisplayHeight = 8192.0,
            Scale = 100.0
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<CropSettings>(json);

        Assert.NotNull(restored);
        Assert.Equal(10000.0, restored.Width);
        Assert.Equal(10000.0, restored.Height);
        Assert.Equal(8192.0, restored.ImageDisplayWidth);
        Assert.Equal(8192.0, restored.ImageDisplayHeight);
        Assert.Equal(100.0, restored.Scale);
    }

    [Fact]
    public void RoundTrip_WithSmallDecimalValues_ShouldPreserveValues()
    {
        var original = new CropSettings
        {
            X = 0.001,
            Y = 0.002,
            Scale = 0.1
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<CropSettings>(json);

        Assert.NotNull(restored);
        Assert.Equal(0.001, restored.X, precision: 10);
        Assert.Equal(0.002, restored.Y, precision: 10);
        Assert.Equal(0.1, restored.Scale, precision: 10);
    }

    [Fact]
    public void Deserialize_WithExtraProperties_ShouldIgnoreThem()
    {
        const string jsonWithExtra = "{\"X\":100,\"Y\":200,\"UnknownProperty\":\"value\",\"AnotherUnknown\":123}";

        var settings = JsonSerializer.Deserialize<CropSettings>(jsonWithExtra);

        Assert.NotNull(settings);
        Assert.Equal(100, settings.X);
        Assert.Equal(200, settings.Y);
    }

    [Fact]
    public void SerializeOptions_CamelCase_ShouldWorkCorrectly()
    {
        var settings = new CropSettings { X = 100, Y = 200 };
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(settings, options);

        Assert.Contains("\"x\"", json);
        Assert.Contains("\"y\"", json);
    }

    [Fact]
    public void DeserializeOptions_CaseInsensitive_ShouldWorkCorrectly()
    {
        const string lowercaseJson = "{\"x\":100,\"y\":200,\"scale\":1.5}";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var settings = JsonSerializer.Deserialize<CropSettings>(lowercaseJson, options);

        Assert.NotNull(settings);
        Assert.Equal(100, settings.X);
        Assert.Equal(200, settings.Y);
        Assert.Equal(1.5, settings.Scale);
    }
}
