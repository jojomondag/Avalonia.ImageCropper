using System.ComponentModel;
using Avalonia.ImageCropper;
using Avalonia.ImageCropper.ViewModels;
using Xunit;

namespace Avalonia.ImageCropper.Tests.ViewModels;

public class ImageCropperViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultOptions()
    {
        var viewModel = new ImageCropperViewModel();

        Assert.NotNull(viewModel.Options);
        Assert.Equal(512, viewModel.Options.OutputSize);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithNullPaths()
    {
        var viewModel = new ImageCropperViewModel();

        Assert.Null(viewModel.OriginalImagePath);
        Assert.Null(viewModel.CropSettingsJson);
        Assert.Null(viewModel.Tag);
    }

    [Fact]
    public void Initialize_ShouldSetOriginalImagePath()
    {
        var viewModel = new ImageCropperViewModel();
        const string path = "/path/to/image.jpg";

        viewModel.Initialize(originalImagePath: path);

        Assert.Equal(path, viewModel.OriginalImagePath);
    }

    [Fact]
    public void Initialize_ShouldSetCropSettingsJson()
    {
        var viewModel = new ImageCropperViewModel();
        const string json = "{\"Scale\":1.5}";

        viewModel.Initialize(cropSettingsJson: json);

        Assert.Equal(json, viewModel.CropSettingsJson);
    }

    [Fact]
    public void Initialize_ShouldSetTag()
    {
        var viewModel = new ImageCropperViewModel();
        var tag = new { Id = 123, Name = "Test" };

        viewModel.Initialize(tag: tag);

        Assert.Equal(tag, viewModel.Tag);
    }

    [Fact]
    public void Initialize_ShouldSetAllParameters()
    {
        var viewModel = new ImageCropperViewModel();
        const string path = "/path/to/image.png";
        const string json = "{\"X\":10,\"Y\":20}";
        var tag = "custom-tag";

        viewModel.Initialize(path, json, tag);

        Assert.Equal(path, viewModel.OriginalImagePath);
        Assert.Equal(json, viewModel.CropSettingsJson);
        Assert.Equal(tag, viewModel.Tag);
    }

    [Fact]
    public void CancelCommand_ShouldRaiseCancelRequestedEvent()
    {
        var viewModel = new ImageCropperViewModel();
        var eventRaised = false;

        viewModel.CancelRequested += (sender, args) => eventRaised = true;
        viewModel.CancelCommand.Execute(null);

        Assert.True(eventRaised);
    }

    [Fact]
    public void CancelCommand_ShouldPassCorrectSender()
    {
        var viewModel = new ImageCropperViewModel();
        object? capturedSender = null;

        viewModel.CancelRequested += (sender, args) => capturedSender = sender;
        viewModel.CancelCommand.Execute(null);

        Assert.Same(viewModel, capturedSender);
    }

    [Fact]
    public void NotifyImageSaved_ShouldRaiseImageSavedEvent()
    {
        var viewModel = new ImageCropperViewModel();
        var eventRaised = false;

        viewModel.ImageSaved += (sender, result) => eventRaised = true;
        viewModel.NotifyImageSaved(new CropResult());

        Assert.True(eventRaised);
    }

    [Fact]
    public void NotifyImageSaved_ShouldPassCropResultToEventHandler()
    {
        var viewModel = new ImageCropperViewModel();
        CropResult? capturedResult = null;
        var expectedResult = new CropResult
        {
            SavedPath = "/saved/path.png",
            OriginalImagePath = "/original/path.jpg"
        };

        viewModel.ImageSaved += (sender, result) => capturedResult = result;
        viewModel.NotifyImageSaved(expectedResult);

        Assert.NotNull(capturedResult);
        Assert.Equal(expectedResult.SavedPath, capturedResult.SavedPath);
        Assert.Equal(expectedResult.OriginalImagePath, capturedResult.OriginalImagePath);
    }

    [Fact]
    public void NotifyImageSaved_ShouldPassCorrectSender()
    {
        var viewModel = new ImageCropperViewModel();
        object? capturedSender = null;

        viewModel.ImageSaved += (sender, result) => capturedSender = sender;
        viewModel.NotifyImageSaved(new CropResult());

        Assert.Same(viewModel, capturedSender);
    }

    [Fact]
    public void PropertyChanged_ShouldFireForOriginalImagePath()
    {
        var viewModel = new ImageCropperViewModel();
        var propertyChangedCount = 0;
        string? changedProperty = null;

        viewModel.PropertyChanged += (sender, args) =>
        {
            propertyChangedCount++;
            changedProperty = args.PropertyName;
        };

        viewModel.Initialize(originalImagePath: "/new/path.jpg");

        Assert.True(propertyChangedCount >= 1);
    }

    [Fact]
    public void PropertyChanged_ShouldFireWhenSettingOptionsDirectly()
    {
        var viewModel = new ImageCropperViewModel();
        var propertyNames = new List<string>();

        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != null)
                propertyNames.Add(args.PropertyName);
        };

        viewModel.Options = new ImageCropperOptions { OutputSize = 1024 };

        Assert.Contains("Options", propertyNames);
    }

    [Fact]
    public void Options_ShouldBeSettable()
    {
        var viewModel = new ImageCropperViewModel();
        var newOptions = new ImageCropperOptions
        {
            OutputSize = 256,
            CropShape = CropShape.Square,
            AllowRotation = false
        };

        viewModel.Options = newOptions;

        Assert.Equal(256, viewModel.Options.OutputSize);
        Assert.Equal(CropShape.Square, viewModel.Options.CropShape);
        Assert.False(viewModel.Options.AllowRotation);
    }

    [Fact]
    public void CancelRequested_ShouldNotThrowWhenNoSubscribers()
    {
        var viewModel = new ImageCropperViewModel();

        var exception = Record.Exception(() => viewModel.CancelCommand.Execute(null));

        Assert.Null(exception);
    }

    [Fact]
    public void NotifyImageSaved_ShouldNotThrowWhenNoSubscribers()
    {
        var viewModel = new ImageCropperViewModel();

        var exception = Record.Exception(() => viewModel.NotifyImageSaved(new CropResult()));

        Assert.Null(exception);
    }
}
