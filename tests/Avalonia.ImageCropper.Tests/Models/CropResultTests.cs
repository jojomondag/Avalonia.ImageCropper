using Avalonia.ImageCropper;
using Xunit;

namespace Avalonia.ImageCropper.Tests.Models;

public class CropResultTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNullValues()
    {
        var result = new CropResult();

        Assert.Null(result.SavedPath);
        Assert.Null(result.OriginalImagePath);
        Assert.Null(result.CropSettingsJson);
        Assert.Null(result.CroppedBitmap);
    }

    [Fact]
    public void SavedPath_ShouldSetAndGetCorrectly()
    {
        var result = new CropResult();
        const string path = "/path/to/saved/image.png";

        result.SavedPath = path;

        Assert.Equal(path, result.SavedPath);
    }

    [Fact]
    public void OriginalImagePath_ShouldSetAndGetCorrectly()
    {
        var result = new CropResult();
        const string path = "/path/to/original/image.jpg";

        result.OriginalImagePath = path;

        Assert.Equal(path, result.OriginalImagePath);
    }

    [Fact]
    public void CropSettingsJson_ShouldSetAndGetCorrectly()
    {
        var result = new CropResult();
        const string json = "{\"X\":10,\"Y\":20}";

        result.CropSettingsJson = json;

        Assert.Equal(json, result.CropSettingsJson);
    }

    [Fact]
    public void AllProperties_ShouldBeSettableIndependently()
    {
        var result = new CropResult
        {
            SavedPath = "/saved/path.png",
            OriginalImagePath = "/original/path.jpg",
            CropSettingsJson = "{\"Scale\":1.5}"
        };

        Assert.Equal("/saved/path.png", result.SavedPath);
        Assert.Equal("/original/path.jpg", result.OriginalImagePath);
        Assert.Equal("{\"Scale\":1.5}", result.CropSettingsJson);
    }
}
