using Avalonia.Controls;
using Avalonia.ImageCropper.ViewModels;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Avalonia.ImageCropper.Controls;

/// <summary>
/// A powerful image cropper control for Avalonia with circular/square crop support,
/// rotation, and image gallery.
/// </summary>
public partial class ImageCropper : UserControl
{
    private CropArea? _cropArea;
    private CropPreview? _cropPreview;
    private ImageGallery? _imageGallery;
    private ImageCropperOptions _options = new();

    /// <summary>
    /// Gets or sets the cropper options.
    /// </summary>
    public ImageCropperOptions Options
    {
        get => _options;
        set
        {
            _options = value;
            ApplyOptions();
        }
    }

    /// <summary>
    /// Gets the ViewModel.
    /// </summary>
    public ImageCropperViewModel? ViewModel => DataContext as ImageCropperViewModel;

    /// <summary>
    /// Event raised when an image is saved.
    /// </summary>
    public event EventHandler<CropResult>? ImageSaved;

    /// <summary>
    /// Event raised when cancel is requested.
    /// </summary>
    public event EventHandler? CancelRequested;

    /// <summary>
    /// Event raised when an original image is selected (from file picker or gallery).
    /// </summary>
    public event EventHandler<string>? OriginalImageSelected;

    /// <summary>
    /// Initializes a new instance of the ImageCropper control.
    /// </summary>
    public ImageCropper()
    {
        InitializeComponent();
        InitializeControls();
        AttachKeyHandlers();
    }

    private void InitializeControls()
    {
        _cropArea = this.FindControl<CropArea>("CropAreaControl");
        _cropPreview = this.FindControl<CropPreview>("CropPreviewControl");
        _imageGallery = this.FindControl<ImageGallery>("ImageGalleryControl");

        if (_cropArea != null)
        {
            _cropArea.ImageAreaClicked += async (s, e) => await SelectImageAsync();
            _cropArea.CropChanged += OnCropChanged;
        }

        if (_cropPreview != null)
        {
            _cropPreview.BackClicked += (s, e) => HandleCancel();
            _cropPreview.RotateLeftClicked += (s, e) => _cropArea?.Rotate(-90);
            _cropPreview.RotateRightClicked += (s, e) => _cropArea?.Rotate(90);
            _cropPreview.ResetClicked += (s, e) => _cropArea?.ResetCrop();
            _cropPreview.SaveClicked += async (s, e) => await SaveAsync();
            _cropPreview.CancelClicked += (s, e) => HandleCancel();
        }

        if (_imageGallery != null)
        {
            _imageGallery.ImageSelected += async (s, path) => await LoadImageAsync(path);
            _imageGallery.ImageDeleted += OnImageDeleted;
        }

        Loaded += async (s, e) => await LoadGalleryAsync();
    }

    private void AttachKeyHandlers()
    {
        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                HandleCancel();
                e.Handled = true;
            }
        };
    }

    private void HandleCancel()
    {
        _cropArea?.Reset();
        CancelRequested?.Invoke(this, EventArgs.Empty);
        ViewModel?.CancelCommand.Execute(null);
    }

    private void OnCropChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
    }

    private void OnImageDeleted(object? sender, string path)
    {
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
                _ = LoadGalleryAsync();
            }
            catch { }
        }
    }

    private void ApplyOptions()
    {
        _cropArea?.SetOptions(_options);

        var contentGrid = this.FindControl<Grid>("ContentGrid");
        if (contentGrid != null && contentGrid.ColumnDefinitions.Count > 1)
        {
            contentGrid.ColumnDefinitions[1].Width = _options.ShowImageGallery
                ? new GridLength(340)
                : new GridLength(0);
        }

        _cropPreview?.SetRotationEnabled(_options.AllowRotation);
    }

    /// <summary>
    /// Loads an image from the specified path.
    /// </summary>
    /// <param name="path">The path to the image file.</param>
    /// <param name="cropSettings">Optional crop settings to restore.</param>
    public async Task LoadImageAsync(string path, CropSettings? cropSettings = null)
    {
        if (!File.Exists(path)) return;

        await (_cropArea?.LoadImageAsync(path, cropSettings) ?? Task.CompletedTask);
        OriginalImageSelected?.Invoke(this, path);
        UpdatePreview();
    }

    /// <summary>
    /// Loads an image from the specified path with JSON crop settings.
    /// </summary>
    /// <param name="path">The path to the image file.</param>
    /// <param name="cropSettingsJson">Optional JSON crop settings to restore.</param>
    public async Task LoadImageAsync(string path, string? cropSettingsJson)
    {
        CropSettings? settings = null;
        if (!string.IsNullOrEmpty(cropSettingsJson))
        {
            try
            {
                settings = JsonSerializer.Deserialize<CropSettings>(cropSettingsJson);
            }
            catch { }
        }
        await LoadImageAsync(path, settings);
    }

    /// <summary>
    /// Opens a file picker to select an image.
    /// </summary>
    public async Task SelectImageAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var filePickerOptions = new FilePickerOpenOptions
        {
            Title = "Select Image",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Image Files")
                {
                    Patterns = _options.SupportedImageExtensions.Select(ext => $"*{ext}").ToArray()
                }
            }
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(filePickerOptions);
        if (files.Count > 0)
        {
            var file = files[0];
            var localPath = file.Path.LocalPath;

            // Save to originals directory if configured
            if (!string.IsNullOrEmpty(_options.OriginalsDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_options.OriginalsDirectory);
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var destPath = Path.Combine(_options.OriginalsDirectory, $"{timestamp}_{file.Name}");

                    await using var sourceStream = await file.OpenReadAsync();
                    await using var destStream = File.Create(destPath);
                    await sourceStream.CopyToAsync(destStream);

                    localPath = destPath;
                }
                catch { }
            }

            await LoadImageAsync(localPath);
            await LoadGalleryAsync();
        }
    }

    /// <summary>
    /// Saves the cropped image.
    /// </summary>
    /// <returns>The crop result, or null if save failed.</returns>
    public async Task<CropResult?> SaveAsync()
    {
        if (_cropArea == null) return null;

        var croppedBitmap = _cropArea.CreateCroppedImage();
        if (croppedBitmap == null) return null;

        string? savePath = null;

        if (!string.IsNullOrEmpty(_options.OutputDirectory))
        {
            Directory.CreateDirectory(_options.OutputDirectory);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            savePath = Path.Combine(_options.OutputDirectory, $"cropped_{timestamp}.png");
        }
        else
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save Cropped Image",
                    DefaultExtension = "png",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("PNG Image") { Patterns = new[] { "*.png" } }
                    }
                });
                savePath = file?.Path.LocalPath;
            }
        }

        if (string.IsNullOrEmpty(savePath)) return null;

        try
        {
            await using var stream = File.Create(savePath);
            croppedBitmap.Save(stream);

            var result = new CropResult
            {
                SavedPath = savePath,
                OriginalImagePath = _cropArea.CurrentOriginalImagePath,
                CropSettingsJson = GetCropSettingsJson(),
                CroppedBitmap = croppedBitmap
            };

            ImageSaved?.Invoke(this, result);
            ViewModel?.NotifyImageSaved(result);

            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the current crop settings as JSON.
    /// </summary>
    public string? GetCropSettingsJson()
    {
        var settings = _cropArea?.GetCropSettings();
        return settings != null ? JsonSerializer.Serialize(settings) : null;
    }

    /// <summary>
    /// Gets the current crop settings.
    /// </summary>
    public CropSettings? GetCropSettings()
    {
        return _cropArea?.GetCropSettings();
    }

    /// <summary>
    /// Refreshes the image gallery.
    /// </summary>
    public async Task LoadGalleryAsync()
    {
        if (_imageGallery == null || string.IsNullOrEmpty(_options.OriginalsDirectory)) return;

        try
        {
            if (!Directory.Exists(_options.OriginalsDirectory))
            {
                Directory.CreateDirectory(_options.OriginalsDirectory);
            }

            var images = Directory.GetFiles(_options.OriginalsDirectory)
                .Where(f => _options.SupportedImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderByDescending(File.GetLastWriteTime)
                .ToArray();

            await _imageGallery.LoadImagesAsync(images);
        }
        catch { }
    }

    private void UpdatePreview()
    {
        if (_cropArea == null || _cropPreview == null) return;
        var preview = _cropArea.CreatePreviewImage();
        _cropPreview.UpdatePreview(preview);
    }
}
