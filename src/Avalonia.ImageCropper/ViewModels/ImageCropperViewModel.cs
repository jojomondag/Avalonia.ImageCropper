using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.ImageCropper.ViewModels;

/// <summary>
/// ViewModel for the ImageCropper control.
/// </summary>
public partial class ImageCropperViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _originalImagePath;

    [ObservableProperty]
    private string? _cropSettingsJson;

    [ObservableProperty]
    private ImageCropperOptions _options = new();

    [ObservableProperty]
    private object? _tag;

    /// <summary>
    /// Event raised when cancel is requested.
    /// </summary>
    public event EventHandler? CancelRequested;

    /// <summary>
    /// Event raised when an image is saved.
    /// </summary>
    public event EventHandler<CropResult>? ImageSaved;

    /// <summary>
    /// Initializes a new instance of the ImageCropperViewModel.
    /// </summary>
    public ImageCropperViewModel()
    {
    }

    /// <summary>
    /// Initializes the view model with an existing image and optional crop settings.
    /// </summary>
    /// <param name="originalImagePath">Path to the original image.</param>
    /// <param name="cropSettingsJson">Optional JSON crop settings to restore.</param>
    /// <param name="tag">Optional tag data to associate with this crop session.</param>
    public void Initialize(string? originalImagePath = null, string? cropSettingsJson = null, object? tag = null)
    {
        OriginalImagePath = originalImagePath;
        CropSettingsJson = cropSettingsJson;
        Tag = tag;
    }

    /// <summary>
    /// Requests cancellation.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Notifies that an image was saved successfully.
    /// </summary>
    /// <param name="result">The crop result.</param>
    public void NotifyImageSaved(CropResult result)
    {
        ImageSaved?.Invoke(this, result);
    }
}
