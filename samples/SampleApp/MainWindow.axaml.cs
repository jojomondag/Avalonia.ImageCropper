using Avalonia.Controls;
using Avalonia.ImageCropper;
using Avalonia.ImageCropper.Controls;
using Avalonia.Interactivity;
using System;
using System.IO;

namespace SampleApp;

public partial class MainWindow : Window
{
    private readonly ImageCropperOptions _options;
    private bool _initialized;

    public MainWindow()
    {
        // Initialize options before InitializeComponent so they're ready when events fire
        var baseDir = Path.Combine(Path.GetTempPath(), "ImageCropperSample");
        _options = new ImageCropperOptions
        {
            OutputSize = 512,
            CropShape = CropShape.Circle,
            ShowImageGallery = true,
            AllowRotation = true,
            OriginalsDirectory = Path.Combine(baseDir, "Originals"),
            OutputDirectory = Path.Combine(baseDir, "Cropped")
        };

        // Ensure directories exist
        Directory.CreateDirectory(_options.OriginalsDirectory);
        Directory.CreateDirectory(_options.OutputDirectory);

        InitializeComponent();

        // Apply options to the cropper
        ImageCropperControl.Options = _options;

        // Subscribe to events
        ImageCropperControl.ImageSaved += OnImageSaved;
        ImageCropperControl.CancelRequested += OnCancelRequested;
        ImageCropperControl.OriginalImageSelected += OnOriginalImageSelected;

        _initialized = true;
        UpdateStatus("Ready - Click on the image area to select an image or use the gallery");
    }

    private void OnCropShapeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;

        if (CropShapeCombo?.SelectedIndex == 0)
        {
            _options.CropShape = CropShape.Circle;
        }
        else
        {
            _options.CropShape = CropShape.Square;
        }
        ImageCropperControl.Options = _options;
        UpdateStatus($"Crop shape changed to {_options.CropShape}");
    }

    private void OnOutputSizeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;

        if (OutputSizeCombo?.SelectedItem is ComboBoxItem item && item.Tag is string sizeStr)
        {
            if (int.TryParse(sizeStr, out var size))
            {
                _options.OutputSize = size;
                ImageCropperControl.Options = _options;
                UpdateStatus($"Output size changed to {size}px");
            }
        }
    }

    private void OnShowGalleryChanged(object? sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        _options.ShowImageGallery = ShowGalleryToggle?.IsChecked ?? true;
        ImageCropperControl.Options = _options;
        UpdateStatus(_options.ShowImageGallery ? "Gallery shown" : "Gallery hidden");
    }

    private void OnAllowRotationChanged(object? sender, RoutedEventArgs e)
    {
        if (!_initialized) return;

        _options.AllowRotation = AllowRotationToggle?.IsChecked ?? true;
        ImageCropperControl.Options = _options;
        UpdateStatus(_options.AllowRotation ? "Rotation enabled" : "Rotation disabled");
    }

    private void OnImageSaved(object? sender, CropResult result)
    {
        UpdateStatus($"Image saved successfully!");
        if (result.SavedPath != null)
        {
            LastSavedText.Text = $"Last saved: {Path.GetFileName(result.SavedPath)}";
        }
    }

    private void OnCancelRequested(object? sender, EventArgs e)
    {
        UpdateStatus("Operation cancelled");
    }

    private void OnOriginalImageSelected(object? sender, string path)
    {
        UpdateStatus($"Loaded: {Path.GetFileName(path)}");
    }

    private void UpdateStatus(string message)
    {
        if (StatusText != null)
        {
            StatusText.Text = message;
        }
    }
}
