using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Avalonia.ImageCropper.Controls;

/// <summary>
/// Control that displays a gallery of available images.
/// </summary>
public partial class ImageGallery : UserControl
{
    private ItemsControl? _imageList;
    private readonly Dictionary<string, Bitmap> _thumbnailCache = new();

    /// <summary>
    /// Event raised when an image is selected.
    /// </summary>
    public event EventHandler<string>? ImageSelected;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageGallery"/> class.
    /// </summary>
    public ImageGallery()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _imageList = this.FindControl<ItemsControl>("ImageList");
    }

    /// <summary>
    /// Loads images into the gallery.
    /// </summary>
    public async Task LoadImagesAsync(string[] imagePaths)
    {
        if (_imageList == null) return;

        var items = new List<Control>();

        foreach (var path in imagePaths)
        {
            if (!File.Exists(path)) continue;

            try
            {
                var thumbnail = await GetThumbnailAsync(path);
                if (thumbnail == null) continue;

                var card = CreateImageCard(path, thumbnail);
                items.Add(card);
            }
            catch { }
        }

        _imageList.ItemsSource = items;
    }

    private async Task<Bitmap?> GetThumbnailAsync(string path)
    {
        if (_thumbnailCache.TryGetValue(path, out var cached))
            return cached;

        return await Task.Run(() =>
        {
            try
            {
                using var stream = File.OpenRead(path);
                var original = new Bitmap(stream);

                const int thumbSize = 80;
                var scale = Math.Min((double)thumbSize / original.PixelSize.Width,
                                     (double)thumbSize / original.PixelSize.Height);
                var newWidth = (int)(original.PixelSize.Width * scale);
                var newHeight = (int)(original.PixelSize.Height * scale);

                var thumbnail = original.CreateScaledBitmap(
                    new PixelSize(newWidth, newHeight),
                    BitmapInterpolationMode.MediumQuality);

                original.Dispose();

                _thumbnailCache[path] = thumbnail;
                return thumbnail;
            }
            catch
            {
                return null;
            }
        });
    }

    private Control CreateImageCard(string path, Bitmap thumbnail)
    {
        const int size = 70;
        const int radius = size / 2;

        // Outer border for the visible circular frame
        var border = new Border
        {
            Width = size,
            Height = size,
            Margin = new Thickness(4),
            CornerRadius = new CornerRadius(radius),
            Cursor = new Cursor(StandardCursorType.Hand),
            Background = Brushes.White,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(2)
        };

        // Inner container to clip the image
        var imageContainer = new Border
        {
            CornerRadius = new CornerRadius(radius - 2),
            ClipToBounds = true
        };

        var image = new Image
        {
            Source = thumbnail,
            Stretch = Stretch.UniformToFill
        };

        imageContainer.Child = image;
        border.Child = imageContainer;
        border.Tag = path;

        border.PointerPressed += (s, e) =>
        {
            if (e.GetCurrentPoint(border).Properties.IsLeftButtonPressed)
            {
                ImageSelected?.Invoke(this, path);
                e.Handled = true;
            }
        };

        border.PointerEntered += (s, e) =>
        {
            border.BorderBrush = Brushes.DodgerBlue;
            border.BorderThickness = new Thickness(3);
        };

        border.PointerExited += (s, e) =>
        {
            border.BorderBrush = Brushes.LightGray;
            border.BorderThickness = new Thickness(2);
        };

        return border;
    }

    /// <summary>
    /// Clears the thumbnail cache.
    /// </summary>
    public void ClearCache()
    {
        foreach (var bitmap in _thumbnailCache.Values)
        {
            bitmap.Dispose();
        }
        _thumbnailCache.Clear();
    }
}
