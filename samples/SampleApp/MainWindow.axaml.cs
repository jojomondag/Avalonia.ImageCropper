using Avalonia.Controls;
using Avalonia.ImageCropper;
using Avalonia.ImageCropper.Controls;
using System;
using System.IO;

namespace SampleApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var cropper = this.FindControl<ImageCropper>("ImageCropperControl");
        if (cropper != null)
        {
            // Configure the cropper
            cropper.Options = new ImageCropperOptions
            {
                OutputSize = 512,
                CropShape = CropShape.Circle,
                ShowImageGallery = true,
                AllowRotation = true,
                OriginalsDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ImageCropperSample", "Originals"),
                OutputDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ImageCropperSample", "Cropped")
            };

            // Handle events
            cropper.ImageSaved += OnImageSaved;
            cropper.CancelRequested += OnCancelRequested;
        }
    }

    private void OnImageSaved(object? sender, CropResult result)
    {
        Console.WriteLine($"Image saved to: {result.SavedPath}");
        Console.WriteLine($"Original: {result.OriginalImagePath}");
        Console.WriteLine($"Crop settings: {result.CropSettingsJson}");
    }

    private void OnCancelRequested(object? sender, EventArgs e)
    {
        Console.WriteLine("Crop cancelled");
        Close();
    }
}
