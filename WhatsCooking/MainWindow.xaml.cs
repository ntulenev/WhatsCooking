using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;

using WhatsCooking.ViewModels;

namespace WhatsCooking;

/// <summary>
/// Main pull request dashboard window.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Main window is created by dependency injection.")]
internal sealed partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">Main dashboard view model.</param>
    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApplyTheme(viewModel.IsLightTheme);
        SourceInitialized += OnSourceInitialized;
        SystemParameters.StaticPropertyChanged += OnSystemParametersPropertyChanged;
        Closed += OnClosed;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        ApplyWindowFrameTheme(DataContext is MainViewModel { IsLightTheme: true });
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsLightTheme)
            && sender is MainViewModel viewModel)
        {
            ApplyTheme(viewModel.IsLightTheme);
        }
    }

    /// <summary>
    /// Shows the modal dialog overlay until the returned scope is disposed.
    /// </summary>
    /// <returns>Scope that hides the overlay when disposed.</returns>
    internal IDisposable ShowDialogOverlay()
    {
        _dialogOverlayScopes++;
        DialogOverlay.Visibility = Visibility.Visible;
        return new DialogOverlayScope(this);
    }

    private void ApplyTheme(bool isLightTheme)
    {
        if (SystemParameters.HighContrast)
        {
            ApplyHighContrastTheme();
            ApplyWindowFrameTheme(isLightTheme);
            return;
        }

        if (isLightTheme)
        {
            SetBrushColor("BgBrush", Color.FromRgb(0xEF, 0xEF, 0xEF));
            SetBrushColor("PanelBrush", Color.FromRgb(0xF7, 0xF7, 0xF7));
            SetBrushColor("PanelAltBrush", Color.FromRgb(0xDD, 0xDD, 0xDD));
            SetBrushColor("BorderBrushDark", Color.FromRgb(0xC9, 0xC9, 0xC9));
            SetBrushColor("TextBrush", Color.FromRgb(0x00, 0x00, 0x00));
            SetBrushColor("MutedBrush", Color.FromRgb(0x63, 0x63, 0x63));
            SetBrushColor("AccentBrush", Color.FromRgb(0x00, 0x52, 0xCC));
            SetBrushColor("LinkBrush", Color.FromRgb(0x00, 0x4A, 0xB8));
            SetBrushColor("DangerBrush", Color.FromRgb(0xD0, 0x00, 0x00));
            SetBrushColor("SuccessBrush", Color.FromRgb(0x17, 0x78, 0x00));
            SetBrushColor("ControlHoverBrush", Color.FromRgb(0xE7, 0xE7, 0xE7));
            SetBrushColor("ControlPressedBrush", Color.FromRgb(0xD2, 0xD2, 0xD2));
            SetBrushColor("InputBrush", Color.FromRgb(0xFC, 0xFC, 0xFC));
            SetBrushColor("HeaderPanelBrush", Color.FromRgb(0xD8, 0xD8, 0xD8));
            SetBrushColor("SubtleBorderBrush", Color.FromRgb(0xC4, 0xC4, 0xC4));
            SetBrushColor("DataGridAltRowBrush", Color.FromRgb(0xF0, 0xF0, 0xF0));
            SetBrushColor("DataGridHorizontalLineBrush", Color.FromRgb(0xD7, 0xD7, 0xD7));
            SetBrushColor("DataGridVerticalLineBrush", Color.FromRgb(0xE1, 0xE1, 0xE1));
            SetBrushColor("DataGridHeaderSelectedBrush", Color.FromRgb(0xC9, 0xDD, 0xF2));
            SetBrushColor("DisabledButtonBrush", Color.FromRgb(0xE3, 0xE3, 0xE3));
            SetBrushColor("DisabledTextBrush", Color.FromRgb(0x88, 0x88, 0x88));
            SetBrushColor("PrimaryButtonBrush", Color.FromRgb(0x00, 0x65, 0xB8));
            SetBrushColor("PrimaryButtonBorderBrush", Color.FromRgb(0x00, 0x4E, 0x8C));
            SetBrushColor("ToggleTrackBrush", Color.FromRgb(0xBC, 0xBC, 0xBC));
            SetBrushColor("ToggleThumbBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
            SetBrushColor("ToggleCheckedTrackBrush", Color.FromRgb(0x00, 0x65, 0xB8));
            SetBrushColor("RequestChangesBadgeBackgroundBrush", Color.FromRgb(0xFF, 0xE8, 0xE3));
            SetBrushColor("RequestChangesBadgeBorderBrush", Color.FromRgb(0xE6, 0xA0, 0x91));
            SetBrushColor("RequestChangesBadgeTextBrush", Color.FromRgb(0x9A, 0x2F, 0x18));
            SetBrushColor("ApprovalBadgeBackgroundBrush", Color.FromRgb(0xE4, 0xF4, 0xE7));
            SetBrushColor("ApprovalBadgeBorderBrush", Color.FromRgb(0x91, 0xC9, 0x9B));
            SetBrushColor("ActivityBadgeBackgroundBrush", Color.FromRgb(0xF0, 0xE8, 0xFA));
            SetBrushColor("ActivityBadgeBorderBrush", Color.FromRgb(0xBA, 0xA2, 0xD4));
            SetBrushColor("ActivityBadgeTextBrush", Color.FromRgb(0x63, 0x3C, 0x85));
        }
        else
        {
            SetBrushColor("BgBrush", Color.FromRgb(0x1E, 0x1E, 0x1E));
            SetBrushColor("PanelBrush", Color.FromRgb(0x25, 0x25, 0x26));
            SetBrushColor("PanelAltBrush", Color.FromRgb(0x2D, 0x2D, 0x30));
            SetBrushColor("BorderBrushDark", Color.FromRgb(0x3C, 0x3C, 0x3C));
            SetBrushColor("TextBrush", Color.FromRgb(0xCC, 0xCC, 0xCC));
            SetBrushColor("MutedBrush", Color.FromRgb(0x8B, 0x8B, 0x8B));
            SetBrushColor("AccentBrush", Color.FromRgb(0x37, 0x94, 0xFF));
            SetBrushColor("LinkBrush", Color.FromRgb(0x4F, 0xC1, 0xFF));
            SetBrushColor("DangerBrush", Color.FromRgb(0xF4, 0x87, 0x71));
            SetBrushColor("SuccessBrush", Color.FromRgb(0x89, 0xD1, 0x85));
            SetBrushColor("ControlHoverBrush", Color.FromRgb(0x34, 0x34, 0x38));
            SetBrushColor("ControlPressedBrush", Color.FromRgb(0x3F, 0x3F, 0x46));
            SetBrushColor("InputBrush", Color.FromRgb(0x1F, 0x1F, 0x1F));
            SetBrushColor("HeaderPanelBrush", Color.FromArgb(0xDD, 0x25, 0x25, 0x26));
            SetBrushColor("SubtleBorderBrush", Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF));
            SetBrushColor("DataGridAltRowBrush", Color.FromRgb(0x28, 0x28, 0x2B));
            SetBrushColor("DataGridHorizontalLineBrush", Color.FromRgb(0x2D, 0x2D, 0x30));
            SetBrushColor("DataGridVerticalLineBrush", Color.FromRgb(0x25, 0x25, 0x26));
            SetBrushColor("DataGridHeaderSelectedBrush", Color.FromRgb(0x17, 0x3B, 0x57));
            SetBrushColor("DisabledButtonBrush", Color.FromRgb(0x20, 0x20, 0x24));
            SetBrushColor("DisabledTextBrush", Color.FromRgb(0x77, 0x77, 0x77));
            SetBrushColor("PrimaryButtonBrush", Color.FromRgb(0x0E, 0x63, 0x9C));
            SetBrushColor("PrimaryButtonBorderBrush", Color.FromRgb(0x11, 0x77, 0xBB));
            SetBrushColor("ToggleTrackBrush", Color.FromRgb(0x3C, 0x3C, 0x3C));
            SetBrushColor("ToggleThumbBrush", Color.FromRgb(0xCC, 0xCC, 0xCC));
            SetBrushColor("ToggleCheckedTrackBrush", Color.FromRgb(0x37, 0x94, 0xFF));
            SetBrushColor("RequestChangesBadgeBackgroundBrush", Color.FromRgb(0x32, 0x22, 0x1F));
            SetBrushColor("RequestChangesBadgeBorderBrush", Color.FromRgb(0x5A, 0x2D, 0x26));
            SetBrushColor("RequestChangesBadgeTextBrush", Color.FromRgb(0xFF, 0x8A, 0x65));
            SetBrushColor("ApprovalBadgeBackgroundBrush", Color.FromRgb(0x1D, 0x33, 0x24));
            SetBrushColor("ApprovalBadgeBorderBrush", Color.FromRgb(0x2F, 0x5F, 0x3B));
            SetBrushColor("ActivityBadgeBackgroundBrush", Color.FromRgb(0x2A, 0x26, 0x36));
            SetBrushColor("ActivityBadgeBorderBrush", Color.FromRgb(0x4F, 0x45, 0x68));
            SetBrushColor("ActivityBadgeTextBrush", Color.FromRgb(0xD7, 0xBA, 0xFF));
        }

        Background = (Brush)FindResource("BgBrush");
        ApplyWindowFrameTheme(isLightTheme);
    }

    private void SetBrushColor(string resourceKey, Color color) => Resources[resourceKey] = new SolidColorBrush(color);

    private void ApplyHighContrastTheme()
    {
        Resources["BgBrush"] = SystemColors.WindowBrush;
        Resources["PanelBrush"] = SystemColors.WindowBrush;
        Resources["PanelAltBrush"] = SystemColors.ControlBrush;
        Resources["BorderBrushDark"] = SystemColors.WindowTextBrush;
        Resources["TextBrush"] = SystemColors.WindowTextBrush;
        Resources["MutedBrush"] = SystemColors.GrayTextBrush;
        Resources["AccentBrush"] = SystemColors.HighlightBrush;
        Resources["LinkBrush"] = SystemColors.HotTrackBrush;
        Resources["DangerBrush"] = SystemColors.WindowTextBrush;
        Resources["SuccessBrush"] = SystemColors.WindowTextBrush;
        Resources["ControlHoverBrush"] = SystemColors.HighlightBrush;
        Resources["ControlPressedBrush"] = SystemColors.HighlightBrush;
        Resources["InputBrush"] = SystemColors.WindowBrush;
        Resources["HeaderPanelBrush"] = SystemColors.ControlBrush;
        Resources["SubtleBorderBrush"] = SystemColors.WindowTextBrush;
        Resources["DataGridAltRowBrush"] = SystemColors.ControlBrush;
        Resources["DataGridHorizontalLineBrush"] = SystemColors.WindowTextBrush;
        Resources["DataGridVerticalLineBrush"] = SystemColors.WindowTextBrush;
        Resources["DataGridHeaderSelectedBrush"] = SystemColors.HighlightBrush;
        Resources["DisabledButtonBrush"] = SystemColors.ControlBrush;
        Resources["DisabledTextBrush"] = SystemColors.GrayTextBrush;
        Resources["PrimaryButtonBrush"] = SystemColors.HighlightBrush;
        Resources["PrimaryButtonBorderBrush"] = SystemColors.HighlightTextBrush;
        Resources["ToggleTrackBrush"] = SystemColors.ControlBrush;
        Resources["ToggleThumbBrush"] = SystemColors.ControlTextBrush;
        Resources["ToggleCheckedTrackBrush"] = SystemColors.HighlightBrush;
        Resources["RequestChangesBadgeBackgroundBrush"] = SystemColors.WindowBrush;
        Resources["RequestChangesBadgeBorderBrush"] = SystemColors.WindowTextBrush;
        Resources["RequestChangesBadgeTextBrush"] = SystemColors.WindowTextBrush;
        Resources["ApprovalBadgeBackgroundBrush"] = SystemColors.WindowBrush;
        Resources["ApprovalBadgeBorderBrush"] = SystemColors.WindowTextBrush;
        Resources["ActivityBadgeBackgroundBrush"] = SystemColors.WindowBrush;
        Resources["ActivityBadgeBorderBrush"] = SystemColors.WindowTextBrush;
        Resources["ActivityBadgeTextBrush"] = SystemColors.WindowTextBrush;
        Background = SystemColors.WindowBrush;
    }

    private void OnSystemParametersPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SystemParameters.HighContrast)
            && DataContext is MainViewModel viewModel)
        {
            ApplyTheme(viewModel.IsLightTheme);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        SourceInitialized -= OnSourceInitialized;
        SystemParameters.StaticPropertyChanged -= OnSystemParametersPropertyChanged;
        Closed -= OnClosed;
    }

    private void ApplyWindowFrameTheme(bool isLightTheme)
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }
        var useDarkMode = isLightTheme ? 0 : 1;

        _ = DwmSetWindowAttribute(
            handle,
            DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref useDarkMode,
            sizeof(int));
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.OemPlus or Key.Add)
        {
            viewModel.IncreaseUiScaleCommand.Execute(null);
            e.Handled = true;
        }
        else if (key is Key.OemMinus or Key.Subtract)
        {
            viewModel.DecreaseUiScaleCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void HideDialogOverlay()
    {
        if (_dialogOverlayScopes <= 0)
        {
            DialogOverlay.Visibility = Visibility.Collapsed;
            return;
        }

        _dialogOverlayScopes--;
        if (_dialogOverlayScopes == 0)
        {
            DialogOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private sealed class DialogOverlayScope : IDisposable
    {
        public DialogOverlayScope(MainWindow owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _owner.HideDialogOverlay();
            _isDisposed = true;
        }

        private readonly MainWindow _owner;

        private bool _isDisposed;
    }

    [LibraryImport("dwmapi.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private readonly MainViewModel _viewModel;

    private int _dialogOverlayScopes;
}
