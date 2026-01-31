# Avalonia.ImageCropper

A powerful, customizable circular/square image cropper control for Avalonia UI applications.

## Features

- **Circular or Square Crop** - Perfect for profile pictures and avatars
- **Drag to Move** - Easily reposition the crop area
- **Resize Handles** - Corner handles for resizing the crop area
- **Rotation Support** - Rotate images with dedicated buttons
- **EXIF Orientation** - Automatically handles image orientation
- **Live Preview** - Real-time preview of the cropped result
- **Image Gallery** - Optional gallery of previously loaded images
- **Configurable Output** - Set custom output size and format
- **Cross-Platform** - Works on Windows, macOS, and Linux

## Installation

```bash
dotnet add package Avalonia.ImageCropper
```

Or via NuGet Package Manager:
```
Install-Package Avalonia.ImageCropper
```

## Quick Start

### 1. Add the namespace to your AXAML

```xml
xmlns:cropper="using:Avalonia.ImageCropper.Controls"
```

### 2. Add the ImageCropper control

```xml
<cropper:ImageCropper x:Name="ImageCropperControl"
                      ImageSaved="OnImageSaved"
                      CancelRequested="OnCancelRequested"/>
```

### 3. Configure and use in code-behind

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Configure the cropper
        ImageCropperControl.Options = new ImageCropperOptions
        {
            OutputSize = 512,
            CropShape = CropShape.Circle,
            ShowImageGallery = true,
            OriginalsDirectory = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "MyApp", "Originals"),
            OutputDirectory = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "MyApp", "Cropped")
        };
    }

    private async void OnImageSaved(object? sender, CropResult result)
    {
        // Handle the saved image
        Console.WriteLine($"Image saved to: {result.SavedPath}");

        // You can also access:
        // result.OriginalImagePath - Path to the original image
        // result.CropSettingsJson - JSON string of crop settings (for restoration)
        // result.CroppedBitmap - The cropped bitmap (before disposal)
    }

    private void OnCancelRequested(object? sender, EventArgs e)
    {
        // Handle cancel (e.g., close the dialog)
    }
}
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `OutputSize` | `int` | 512 | The output size of the cropped image in pixels |
| `MinimumCropSize` | `double` | 50 | Minimum size of the crop area |
| `MaximumCropSize` | `double` | 2000 | Maximum size of the crop area |
| `MaximumCropRatio` | `double` | 0.95 | Maximum ratio of crop size to image size |
| `MaximumDisplayImageSize` | `int` | 4096 | Maximum size for display image (performance) |
| `CropShape` | `CropShape` | Circle | Shape of the crop area (Circle or Square) |
| `ShowImageGallery` | `bool` | true | Whether to show the image gallery |
| `AllowRotation` | `bool` | true | Whether to show rotation buttons |
| `SupportedImageExtensions` | `string[]` | jpg, jpeg, png, bmp, gif, webp | Supported file extensions |
| `PreviewThrottleMs` | `int` | 33 | Preview update throttle (30fps) |
| `OriginalsDirectory` | `string?` | null | Directory to store original images |
| `OutputDirectory` | `string?` | null | Default directory for cropped images |

## Saving and Restoring Crop Settings

You can save and restore crop settings to allow users to re-edit their crops:

```csharp
// Get current crop settings as JSON
string? settingsJson = ImageCropperControl.GetCropSettingsJson();

// Later, restore the crop settings
await ImageCropperControl.LoadImageAsync(originalImagePath, settingsJson);
```

## Loading Images Programmatically

```csharp
// Load an image from file
await ImageCropperControl.LoadImageAsync("/path/to/image.jpg");

// Load with existing crop settings
await ImageCropperControl.LoadImageAsync("/path/to/image.jpg", cropSettings);

// Open file picker
await ImageCropperControl.SelectImageAsync();
```

## Using with MVVM

The control includes a `ImageCropperViewModel` that you can use:

```csharp
var viewModel = new ImageCropperViewModel();
viewModel.Initialize(originalImagePath, existingCropSettingsJson, customTag);

viewModel.ImageSaved += (s, result) =>
{
    // Handle save
};

viewModel.CancelRequested += (s, e) =>
{
    // Handle cancel
};

ImageCropperControl.DataContext = viewModel;
```

## Styling

The control uses Avalonia's theming system and respects your app's theme. You can customize the appearance using standard Avalonia styles.

## Requirements

- .NET 8.0 or later
- Avalonia 11.2.1 or later

## License

MIT License - see [LICENSE](LICENSE) for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Acknowledgments

This package was extracted and adapted from the [TeachersLittleHelper](https://github.com/jojomondag/TeachersLittleHelper) project.
