using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System;

namespace Avalonia.ImageCropper.Controls;

/// <summary>
/// Control that shows the crop preview and action buttons.
/// </summary>
public partial class CropPreview : UserControl
{
    private Image? _previewImage;
    private StackPanel? _actionsPanel;
    private Button? _backButton;
    private Button? _rotateLeftButton;
    private Button? _rotateRightButton;
    private Button? _resetButton;
    private Button? _saveButton;
    private Button? _cancelButton;

    /// <summary>
    /// Event raised when back button is clicked.
    /// </summary>
    public event EventHandler? BackClicked;

    /// <summary>
    /// Event raised when rotate left button is clicked.
    /// </summary>
    public event EventHandler? RotateLeftClicked;

    /// <summary>
    /// Event raised when rotate right button is clicked.
    /// </summary>
    public event EventHandler? RotateRightClicked;

    /// <summary>
    /// Event raised when reset button is clicked.
    /// </summary>
    public event EventHandler? ResetClicked;

    /// <summary>
    /// Event raised when save button is clicked.
    /// </summary>
    public event EventHandler? SaveClicked;

    /// <summary>
    /// Event raised when cancel button is clicked.
    /// </summary>
    public event EventHandler? CancelClicked;

    public CropPreview()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _previewImage = this.FindControl<Image>("PreviewImage");
        _actionsPanel = this.FindControl<StackPanel>("ActionsPanel");
        _backButton = this.FindControl<Button>("BackButton");
        _rotateLeftButton = this.FindControl<Button>("RotateLeftButton");
        _rotateRightButton = this.FindControl<Button>("RotateRightButton");
        _resetButton = this.FindControl<Button>("ResetButton");
        _saveButton = this.FindControl<Button>("SaveButton");
        _cancelButton = this.FindControl<Button>("CancelButton");

        if (_backButton != null) _backButton.Click += (s, e) => BackClicked?.Invoke(this, EventArgs.Empty);
        if (_rotateLeftButton != null) _rotateLeftButton.Click += (s, e) => RotateLeftClicked?.Invoke(this, EventArgs.Empty);
        if (_rotateRightButton != null) _rotateRightButton.Click += (s, e) => RotateRightClicked?.Invoke(this, EventArgs.Empty);
        if (_resetButton != null) _resetButton.Click += (s, e) => ResetClicked?.Invoke(this, EventArgs.Empty);
        if (_saveButton != null) _saveButton.Click += (s, e) => SaveClicked?.Invoke(this, EventArgs.Empty);
        if (_cancelButton != null) _cancelButton.Click += (s, e) => CancelClicked?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Updates the preview image.
    /// </summary>
    public void UpdatePreview(Bitmap? bitmap)
    {
        if (_previewImage != null)
        {
            _previewImage.Source = bitmap;
        }

        ShowActions(bitmap != null);
    }

    /// <summary>
    /// Shows or hides the action buttons.
    /// </summary>
    public void ShowActions(bool show)
    {
        if (_actionsPanel != null)
        {
            _actionsPanel.IsVisible = show;
        }
    }

    /// <summary>
    /// Enables or disables rotation buttons.
    /// </summary>
    public void SetRotationEnabled(bool enabled)
    {
        if (_rotateLeftButton != null) _rotateLeftButton.IsVisible = enabled;
        if (_rotateRightButton != null) _rotateRightButton.IsVisible = enabled;
    }

    /// <summary>
    /// Enables or disables the reset button.
    /// </summary>
    public void SetResetEnabled(bool enabled)
    {
        if (_resetButton != null) _resetButton.IsEnabled = enabled;
    }

    /// <summary>
    /// Enables or disables the save button.
    /// </summary>
    public void SetSaveEnabled(bool enabled)
    {
        if (_saveButton != null) _saveButton.IsEnabled = enabled;
    }
}
