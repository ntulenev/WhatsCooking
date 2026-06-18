using System.Collections;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WhatsCooking.Views;

/// <summary>
/// Application-styled modal dialog.
/// </summary>
internal sealed partial class StyledDialogWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StyledDialogWindow"/> class.
    /// </summary>
    public StyledDialogWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Dialog title.
    /// </summary>
    public string DialogTitle {
        get => (string)GetValue(_dialogTitleProperty);
        set => SetValue(_dialogTitleProperty, value);
    }

    /// <summary>
    /// Dialog message.
    /// </summary>
    public string Message {
        get => (string)GetValue(_messageProperty);
        set => SetValue(_messageProperty, value);
    }

    /// <summary>
    /// Text shown inside the leading status icon.
    /// </summary>
    public string IconText {
        get => (string)GetValue(_iconTextProperty);
        set => SetValue(_iconTextProperty, value);
    }

    /// <summary>
    /// Confirm button text.
    /// </summary>
    public string ConfirmText {
        get => (string)GetValue(_confirmTextProperty);
        set => SetValue(_confirmTextProperty, value);
    }

    /// <summary>
    /// Cancel button text.
    /// </summary>
    public string CancelText {
        get => (string)GetValue(_cancelTextProperty);
        set => SetValue(_cancelTextProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether both confirm and cancel actions are visible.
    /// </summary>
    public bool IsConfirmationDialog {
        get => (bool)GetValue(_isConfirmationDialogProperty);
        set => SetValue(_isConfirmationDialogProperty, value);
    }

    /// <summary>
    /// Shows a styled confirmation dialog.
    /// </summary>
    public static bool ShowConfirmation(Window? owner, string title, string message, string iconText)
    {
        var dialog = Create(owner, title, message, iconText);
        dialog.IsConfirmationDialog = true;
        dialog.ConfirmText = "Yes";
        dialog.CancelText = "No";
        return ShowWithOwnerOverlay(dialog) == true;
    }

    /// <summary>
    /// Shows a styled message dialog.
    /// </summary>
    public static void ShowMessage(Window? owner, string title, string message, string iconText)
    {
        var dialog = Create(owner, title, message, iconText);
        dialog.IsConfirmationDialog = false;
        dialog.ConfirmButton.Visibility = Visibility.Collapsed;
        dialog.CancelText = "OK";
        _ = ShowWithOwnerOverlay(dialog);
    }

    private static StyledDialogWindow Create(Window? owner, string title, string message, string iconText)
    {
        var dialog = new StyledDialogWindow
        {
            Owner = owner,
            DialogTitle = title,
            Message = message,
            IconText = iconText
        };
        dialog.InheritBrushResources(owner);
        return dialog;
    }

    private static bool? ShowWithOwnerOverlay(StyledDialogWindow dialog)
    {
        using var overlay = dialog.Owner is MainWindow owner
            ? owner.ShowDialogOverlay()
            : null;
        return dialog.ShowDialog();
    }

    private void InheritBrushResources(Window? owner)
    {
        if (owner is null)
        {
            return;
        }

        foreach (DictionaryEntry resource in owner.Resources)
        {
            if (resource.Key is string key && resource.Value is Brush brush)
            {
                Resources[key] = brush;
            }
        }

        Background = (Brush)FindResource("BgBrush");
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
        }
    }

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private static readonly DependencyProperty _dialogTitleProperty = DependencyProperty.Register(
        nameof(DialogTitle),
        typeof(string),
        typeof(StyledDialogWindow),
        new PropertyMetadata(string.Empty));

    private static readonly DependencyProperty _messageProperty = DependencyProperty.Register(
        nameof(Message),
        typeof(string),
        typeof(StyledDialogWindow),
        new PropertyMetadata(string.Empty));

    private static readonly DependencyProperty _iconTextProperty = DependencyProperty.Register(
        nameof(IconText),
        typeof(string),
        typeof(StyledDialogWindow),
        new PropertyMetadata("?"));

    private static readonly DependencyProperty _confirmTextProperty = DependencyProperty.Register(
        nameof(ConfirmText),
        typeof(string),
        typeof(StyledDialogWindow),
        new PropertyMetadata("Yes"));

    private static readonly DependencyProperty _cancelTextProperty = DependencyProperty.Register(
        nameof(CancelText),
        typeof(string),
        typeof(StyledDialogWindow),
        new PropertyMetadata("No"));

    private static readonly DependencyProperty _isConfirmationDialogProperty = DependencyProperty.Register(
        nameof(IsConfirmationDialog),
        typeof(bool),
        typeof(StyledDialogWindow),
        new PropertyMetadata(true));
}
