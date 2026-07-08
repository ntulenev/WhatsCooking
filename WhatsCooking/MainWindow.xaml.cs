using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Extensions.Configuration;
using Microsoft.Win32;

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
    /// <param name="configuration">Application configuration.</param>
    public MainWindow(MainViewModel viewModel, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        _viewModel = viewModel;
        _horizontalScrollWheelMultiplier = ReadScrollWheelMultiplier(
            configuration,
            "Ui:HorizontalScrollWheelMultiplier",
            DEFAULT_SCROLL_WHEEL_MULTIPLIER);
        _verticalScrollWheelMultiplier = ReadScrollWheelMultiplier(
            configuration,
            "Ui:VerticalScrollWheelMultiplier",
            DEFAULT_SCROLL_WHEEL_MULTIPLIER);
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApplyTheme(viewModel.ThemeMode);
        SourceInitialized += OnSourceInitialized;
        SystemParameters.StaticPropertyChanged += OnSystemParametersPropertyChanged;
        Closed += OnClosed;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        ApplyWindowFrameTheme(DataContext is MainViewModel { ThemeMode: AppThemeMode.Light });
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ThemeMode)
            && sender is MainViewModel viewModel)
        {
            ApplyTheme(viewModel.ThemeMode);
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

    /// <summary>
    /// Fits the columns of the data grid in the currently selected tab to the current available width.
    /// </summary>
    internal void FitCurrentDataGridColumns()
    {
        if (DashboardTabs.SelectedItem is not TabItem { Content: DependencyObject tabContent })
        {
            return;
        }

        var dataGrid = FindDescendant<DataGrid>(tabContent);
        if (dataGrid is null)
        {
            return;
        }

        FitDataGridColumns(dataGrid);
    }

    private void ApplyTheme(AppThemeMode themeMode)
    {
        var resolvedThemeMode = ResolveThemeMode(themeMode);
        var isLightTheme = resolvedThemeMode == AppThemeMode.Light;
        if (SystemParameters.HighContrast)
        {
            ApplyHighContrastTheme();
            ApplyWindowFrameTheme(isLightTheme);
            return;
        }

        ApplyPalette(ResolvePalette(resolvedThemeMode));

        Background = (Brush)FindResource("BgBrush");
        ApplyWindowFrameTheme(isLightTheme);
    }

    private void SetBrushColor(string resourceKey, Color color) => Resources[resourceKey] = new SolidColorBrush(color);

    private void ApplyPalette(ThemePalette palette)
    {
        SetGradientBrush("BgBrush", palette.Bg, Blend(palette.Bg, palette.Accent, 0.08));
        SetBrushColor("PanelBrush", palette.Panel);
        SetGradientBrush("PanelAltBrush", palette.PanelAlt, Blend(palette.PanelAlt, palette.Accent, 0.06));
        SetBrushColor("BorderBrushDark", palette.Border);
        SetBrushColor("TextBrush", palette.Text);
        SetBrushColor("MutedBrush", palette.Muted);
        SetBrushColor("AccentBrush", palette.Accent);
        SetBrushColor("LinkBrush", palette.Link);
        SetBrushColor("DangerBrush", palette.Danger);
        SetBrushColor("SuccessBrush", palette.Success);
        SetBrushColor("ControlHoverBrush", palette.Hover);
        SetBrushColor("ControlPressedBrush", palette.Pressed);
        SetBrushColor("InputBrush", palette.Input);
        SetGradientBrush("HeaderPanelBrush", palette.Header, Blend(palette.Header, palette.Accent, 0.10));
        SetBrushColor("SubtleBorderBrush", palette.SubtleBorder);
        SetBrushColor("DataGridAltRowBrush", palette.DataGridAltRow);
        SetBrushColor("DataGridHeaderBrush", palette.DataGridHeader);
        SetBrushColor("DataGridHorizontalLineBrush", palette.DataGridHorizontalLine);
        SetBrushColor("DataGridVerticalLineBrush", palette.DataGridVerticalLine);
        SetBrushColor("DataGridHeaderSelectedBrush", palette.DataGridHeaderSelected);
        SetBrushColor("DisabledButtonBrush", palette.DisabledButton);
        SetBrushColor("DisabledTextBrush", palette.DisabledText);
        SetBrushColor("PrimaryButtonBrush", palette.PrimaryButton);
        SetBrushColor("PrimaryButtonBorderBrush", palette.PrimaryButtonBorder);
        SetBrushColor("ToggleTrackBrush", palette.ToggleTrack);
        SetBrushColor("ToggleThumbBrush", palette.ToggleThumb);
        SetBrushColor("ToggleCheckedTrackBrush", palette.ToggleCheckedTrack);
        SetBrushColor("RequestChangesBadgeBackgroundBrush", palette.RequestChangesBadgeBackground);
        SetBrushColor("RequestChangesBadgeBorderBrush", palette.RequestChangesBadgeBorder);
        SetBrushColor("RequestChangesBadgeTextBrush", palette.RequestChangesBadgeText);
        SetBrushColor("ApprovalBadgeBackgroundBrush", palette.ApprovalBadgeBackground);
        SetBrushColor("ApprovalBadgeBorderBrush", palette.ApprovalBadgeBorder);
        SetBrushColor("ActivityBadgeBackgroundBrush", palette.ActivityBadgeBackground);
        SetBrushColor("ActivityBadgeBorderBrush", palette.ActivityBadgeBorder);
        SetBrushColor("ActivityBadgeTextBrush", palette.ActivityBadgeText);
    }

    private void SetGradientBrush(string resourceKey, Color startColor, Color endColor)
    {
        Resources[resourceKey] = new LinearGradientBrush(
            startColor,
            endColor,
            new Point(0, 0),
            new Point(1, 1));
    }

    private static Color Blend(Color baseColor, Color tintColor, double amount)
    {
        var inverse = 1 - amount;
        return Color.FromArgb(
            baseColor.A,
            (byte)((baseColor.R * inverse) + (tintColor.R * amount)),
            (byte)((baseColor.G * inverse) + (tintColor.G * amount)),
            (byte)((baseColor.B * inverse) + (tintColor.B * amount)));
    }

    private static AppThemeMode ResolveThemeMode(AppThemeMode themeMode) =>
        themeMode == AppThemeMode.Os ? DetectOsThemeMode() : themeMode;

    private static AppThemeMode DetectOsThemeMode()
    {
        try
        {
            var value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                0);
            return value is int appsUseLightTheme && appsUseLightTheme > 0
                ? AppThemeMode.Light
                : AppThemeMode.Dark;
        }
        catch (IOException)
        {
            return AppThemeMode.Dark;
        }
        catch (UnauthorizedAccessException)
        {
            return AppThemeMode.Dark;
        }
    }

    private static ThemePalette ResolvePalette(AppThemeMode themeMode) => themeMode switch
    {
        AppThemeMode.Os => _darkPalette,
        AppThemeMode.Light => _lightPalette,
        AppThemeMode.Glass => _darkPalette with
        {
            Bg = Color.FromRgb(0x10, 0x18, 0x24),
            Panel = Color.FromRgb(0x11, 0x1A, 0x24),
            PanelAlt = Color.FromRgb(0x18, 0x22, 0x2B),
            Border = Color.FromRgb(0x5D, 0x76, 0x8D),
            Text = Color.FromRgb(0xF7, 0xFB, 0xFF),
            Muted = Color.FromRgb(0xB5, 0xCC, 0xDE),
            Accent = Color.FromRgb(0x88, 0xA3, 0xFF),
            Link = Color.FromRgb(0xBD, 0xD7, 0xF4),
            Danger = Color.FromRgb(0xFF, 0x7D, 0x6B),
            Success = Color.FromRgb(0x9D, 0xD7, 0xF5),
            Hover = Color.FromRgb(0x26, 0x35, 0x47),
            Pressed = Color.FromRgb(0x32, 0x43, 0x56),
            Input = Color.FromRgb(0x11, 0x1B, 0x25),
            Header = Color.FromRgb(0x13, 0x1B, 0x28),
            SubtleBorder = Color.FromRgb(0x5D, 0x76, 0x8D),
            DataGridAltRow = Color.FromRgb(0x16, 0x20, 0x2D),
            DataGridHeader = Color.FromRgb(0x18, 0x22, 0x2B),
            DataGridHorizontalLine = Color.FromRgb(0x2A, 0x38, 0x49),
            DataGridVerticalLine = Color.FromRgb(0x34, 0x43, 0x55),
            DataGridHeaderSelected = Color.FromRgb(0x24, 0x31, 0x42),
            RequestChangesBadgeBackground = Color.FromRgb(0x4C, 0x36, 0x40),
            RequestChangesBadgeBorder = Color.FromRgb(0x8A, 0x61, 0x75),
            RequestChangesBadgeText = Color.FromRgb(0xFF, 0xC1, 0xD0),
            ApprovalBadgeBackground = Color.FromRgb(0x24, 0x31, 0x42),
            ApprovalBadgeBorder = Color.FromRgb(0x72, 0x8E, 0xA6),
            ActivityBadgeBackground = Color.FromRgb(0x24, 0x31, 0x42),
            ActivityBadgeBorder = Color.FromRgb(0x7E, 0x72, 0xA6),
            ActivityBadgeText = Color.FromRgb(0xC9, 0xDD, 0xED)
        },
        AppThemeMode.Forest => _darkPalette with
        {
            Bg = Color.FromRgb(0x17, 0x1C, 0x19),
            Panel = Color.FromRgb(0x1A, 0x21, 0x1C),
            PanelAlt = Color.FromRgb(0x1D, 0x26, 0x20),
            Border = Color.FromRgb(0x2B, 0x37, 0x30),
            Text = Color.FromRgb(0xED, 0xF3, 0xEE),
            Muted = Color.FromRgb(0x8A, 0x99, 0x8F),
            Accent = Color.FromRgb(0x5A, 0x7D, 0x67),
            Link = Color.FromRgb(0x8F, 0xB8, 0x9D),
            Danger = Color.FromRgb(0xC9, 0x6E, 0x60),
            Success = Color.FromRgb(0x8F, 0xC0, 0x9A),
            Hover = Color.FromRgb(0x20, 0x2A, 0x24),
            Pressed = Color.FromRgb(0x26, 0x31, 0x29),
            Input = Color.FromRgb(0x16, 0x1C, 0x18),
            Header = Color.FromRgb(0x1D, 0x26, 0x20),
            SubtleBorder = Color.FromRgb(0x33, 0x40, 0x39),
            DataGridAltRow = Color.FromRgb(0x1C, 0x24, 0x1E),
            DataGridHeader = Color.FromRgb(0x1D, 0x26, 0x20),
            DataGridHorizontalLine = Color.FromRgb(0x26, 0x32, 0x2A),
            DataGridVerticalLine = Color.FromRgb(0x2B, 0x37, 0x30),
            DataGridHeaderSelected = Color.FromRgb(0x26, 0x31, 0x29),
            RequestChangesBadgeBackground = Color.FromRgb(0x33, 0x2B, 0x2B),
            RequestChangesBadgeBorder = Color.FromRgb(0x8F, 0x67, 0x64),
            RequestChangesBadgeText = Color.FromRgb(0xE1, 0xD1, 0xCE),
            ApprovalBadgeBackground = Color.FromRgb(0x22, 0x2C, 0x26),
            ApprovalBadgeBorder = Color.FromRgb(0x40, 0x52, 0x47),
            ActivityBadgeBackground = Color.FromRgb(0x22, 0x2C, 0x26),
            ActivityBadgeBorder = Color.FromRgb(0x40, 0x52, 0x47),
            ActivityBadgeText = Color.FromRgb(0xC8, 0xD6, 0xCB)
        },
        AppThemeMode.Autumn => _darkPalette with
        {
            Bg = Color.FromRgb(0x26, 0x1C, 0x1E),
            Panel = Color.FromRgb(0x2D, 0x22, 0x23),
            PanelAlt = Color.FromRgb(0x31, 0x24, 0x26),
            Border = Color.FromRgb(0x46, 0x36, 0x35),
            Text = Color.FromRgb(0xF5, 0xE9, 0xD8),
            Muted = Color.FromRgb(0xB9, 0x9C, 0x89),
            Accent = Color.FromRgb(0xC9, 0x75, 0x37),
            Link = Color.FromRgb(0xE8, 0xA4, 0x62),
            Danger = Color.FromRgb(0xE0, 0x7C, 0x4B),
            Success = Color.FromRgb(0x76, 0xC5, 0xB2),
            Hover = Color.FromRgb(0x39, 0x2A, 0x2B),
            Pressed = Color.FromRgb(0x41, 0x2F, 0x30),
            Input = Color.FromRgb(0x24, 0x1A, 0x1C),
            Header = Color.FromRgb(0x31, 0x24, 0x26),
            SubtleBorder = Color.FromRgb(0x4A, 0x39, 0x35),
            DataGridAltRow = Color.FromRgb(0x2A, 0x20, 0x21),
            DataGridHeader = Color.FromRgb(0x31, 0x24, 0x26),
            DataGridHorizontalLine = Color.FromRgb(0x3C, 0x2E, 0x30),
            DataGridVerticalLine = Color.FromRgb(0x46, 0x36, 0x35),
            DataGridHeaderSelected = Color.FromRgb(0x41, 0x2F, 0x30),
            RequestChangesBadgeBackground = Color.FromRgb(0x47, 0x2F, 0x2B),
            RequestChangesBadgeBorder = Color.FromRgb(0xBA, 0x71, 0x5B),
            RequestChangesBadgeText = Color.FromRgb(0xEB, 0xC8, 0xBF),
            ApprovalBadgeBackground = Color.FromRgb(0x2A, 0x42, 0x43),
            ApprovalBadgeBorder = Color.FromRgb(0x76, 0xC5, 0xB2),
            ActivityBadgeBackground = Color.FromRgb(0x35, 0x28, 0x2A),
            ActivityBadgeBorder = Color.FromRgb(0x5D, 0x47, 0x40),
            ActivityBadgeText = Color.FromRgb(0xD9, 0xC3, 0xAF)
        },
        AppThemeMode.DarkPink => _darkPalette with
        {
            Bg = Color.FromRgb(0x24, 0x1D, 0x21),
            Panel = Color.FromRgb(0x2B, 0x23, 0x28),
            PanelAlt = Color.FromRgb(0x34, 0x29, 0x2F),
            Border = Color.FromRgb(0x41, 0x33, 0x3B),
            Text = Color.FromRgb(0xF4, 0xEB, 0xF1),
            Muted = Color.FromRgb(0xA3, 0x8A, 0x97),
            Accent = Color.FromRgb(0xDB, 0x6F, 0x9A),
            Link = Color.FromRgb(0xE8, 0xA7, 0xC4),
            Danger = Color.FromRgb(0xFF, 0x7A, 0x8C),
            Success = Color.FromRgb(0x65, 0xB7, 0xD2),
            Hover = Color.FromRgb(0x3A, 0x2D, 0x34),
            Pressed = Color.FromRgb(0x43, 0x33, 0x3B),
            Input = Color.FromRgb(0x24, 0x1E, 0x22),
            Header = Color.FromRgb(0x34, 0x29, 0x2F),
            SubtleBorder = Color.FromRgb(0x4A, 0x3A, 0x42),
            DataGridAltRow = Color.FromRgb(0x2E, 0x25, 0x2B),
            DataGridHeader = Color.FromRgb(0x34, 0x29, 0x2F),
            DataGridHorizontalLine = Color.FromRgb(0x3D, 0x30, 0x37),
            DataGridVerticalLine = Color.FromRgb(0x41, 0x33, 0x3B),
            DataGridHeaderSelected = Color.FromRgb(0x43, 0x33, 0x3B),
            RequestChangesBadgeBackground = Color.FromRgb(0x40, 0x2D, 0x33),
            RequestChangesBadgeBorder = Color.FromRgb(0xB8, 0x79, 0x86),
            RequestChangesBadgeText = Color.FromRgb(0xE7, 0xCD, 0xD5),
            ApprovalBadgeBackground = Color.FromRgb(0x34, 0x36, 0x46),
            ApprovalBadgeBorder = Color.FromRgb(0x65, 0xB7, 0xD2),
            ActivityBadgeBackground = Color.FromRgb(0x33, 0x28, 0x2E),
            ActivityBadgeBorder = Color.FromRgb(0x5D, 0x47, 0x52),
            ActivityBadgeText = Color.FromRgb(0xD0, 0xC1, 0xCA)
        },
        AppThemeMode.Matrix => _darkPalette with
        {
            Bg = Color.FromRgb(0x02, 0x04, 0x03),
            Panel = Color.FromRgb(0x03, 0x07, 0x04),
            PanelAlt = Color.FromRgb(0x04, 0x08, 0x05),
            Border = Color.FromRgb(0x0A, 0x7A, 0x28),
            Text = Color.FromRgb(0x82, 0xFF, 0x95),
            Muted = Color.FromRgb(0x2D, 0xF4, 0x5B),
            Accent = Color.FromRgb(0x09, 0xCF, 0x3D),
            Link = Color.FromRgb(0x19, 0xFF, 0x4B),
            Danger = Color.FromRgb(0x19, 0xFF, 0x4B),
            Success = Color.FromRgb(0x0D, 0xF2, 0x4F),
            Hover = Color.FromRgb(0x08, 0x11, 0x09),
            Pressed = Color.FromRgb(0x0A, 0x15, 0x0B),
            Input = Color.FromRgb(0x02, 0x06, 0x03),
            Header = Color.FromRgb(0x04, 0x08, 0x05),
            SubtleBorder = Color.FromRgb(0x0B, 0x8B, 0x2E),
            DataGridAltRow = Color.FromRgb(0x04, 0x0A, 0x05),
            DataGridHeader = Color.FromRgb(0x04, 0x08, 0x05),
            DataGridHorizontalLine = Color.FromRgb(0x08, 0x64, 0x20),
            DataGridVerticalLine = Color.FromRgb(0x0A, 0x7A, 0x28),
            DataGridHeaderSelected = Color.FromRgb(0x0A, 0x15, 0x0B),
            RequestChangesBadgeBackground = Color.FromRgb(0x12, 0x0E, 0x0A),
            RequestChangesBadgeBorder = Color.FromRgb(0x58, 0xB7, 0x6C),
            RequestChangesBadgeText = Color.FromRgb(0x9E, 0xE5, 0xA7),
            ApprovalBadgeBackground = Color.FromRgb(0x05, 0x11, 0x08),
            ApprovalBadgeBorder = Color.FromRgb(0x0D, 0xF2, 0x4F),
            ActivityBadgeBackground = Color.FromRgb(0x07, 0x10, 0x09),
            ActivityBadgeBorder = Color.FromRgb(0x0C, 0x7A, 0x28),
            ActivityBadgeText = Color.FromRgb(0x6E, 0xF9, 0x85)
        },
        AppThemeMode.Code => _darkPalette with
        {
            Bg = Color.FromRgb(0x1E, 0x1E, 0x1E),
            Panel = Color.FromRgb(0x25, 0x25, 0x26),
            PanelAlt = Color.FromRgb(0x2D, 0x2D, 0x30),
            Border = Color.FromRgb(0x33, 0x33, 0x37),
            Text = Color.FromRgb(0xF1, 0xF1, 0xF1),
            Muted = Color.FromRgb(0xA6, 0xA6, 0xA6),
            Accent = Color.FromRgb(0x0E, 0x63, 0x9C),
            Link = Color.FromRgb(0x4E, 0xC9, 0xB0),
            Danger = Color.FromRgb(0xCE, 0x91, 0x78),
            Success = Color.FromRgb(0xB5, 0xCE, 0xA8),
            Hover = Color.FromRgb(0x37, 0x37, 0x3D),
            Pressed = Color.FromRgb(0x41, 0x41, 0x47),
            Input = Color.FromRgb(0x1E, 0x1E, 0x1E),
            Header = Color.FromRgb(0x2D, 0x2D, 0x30),
            SubtleBorder = Color.FromRgb(0x3F, 0x3F, 0x46),
            DataGridAltRow = Color.FromRgb(0x28, 0x28, 0x2B),
            DataGridHeader = Color.FromRgb(0x2D, 0x2D, 0x30),
            DataGridHorizontalLine = Color.FromRgb(0x2D, 0x2D, 0x30),
            DataGridVerticalLine = Color.FromRgb(0x25, 0x25, 0x26),
            DataGridHeaderSelected = Color.FromRgb(0x26, 0x4F, 0x78),
            RequestChangesBadgeBackground = Color.FromRgb(0x3A, 0x25, 0x25),
            RequestChangesBadgeBorder = Color.FromRgb(0xD1, 0x69, 0x69),
            RequestChangesBadgeText = Color.FromRgb(0xF1, 0xC4, 0xC4),
            ApprovalBadgeBackground = Color.FromRgb(0x24, 0x31, 0x40),
            ApprovalBadgeBorder = Color.FromRgb(0x3B, 0x78, 0xC8),
            ActivityBadgeBackground = Color.FromRgb(0x2A, 0x2A, 0x2C),
            ActivityBadgeBorder = Color.FromRgb(0x3F, 0x3F, 0x46),
            ActivityBadgeText = Color.FromRgb(0xC8, 0xC8, 0xC8)
        },
        AppThemeMode.Cyberpunk => _darkPalette with
        {
            Bg = Color.FromRgb(0x14, 0x0F, 0x25),
            Panel = Color.FromRgb(0x20, 0x16, 0x35),
            PanelAlt = Color.FromRgb(0x24, 0x1A, 0x39),
            Border = Color.FromRgb(0x33, 0x27, 0x4F),
            Text = Color.FromRgb(0xF7, 0xF3, 0xFF),
            Muted = Color.FromRgb(0x9A, 0x90, 0xD1),
            Accent = Color.FromRgb(0x00, 0xF5, 0xD4),
            Link = Color.FromRgb(0x7E, 0xE7, 0xFF),
            Danger = Color.FromRgb(0xFF, 0x5D, 0xA2),
            Success = Color.FromRgb(0x00, 0xF5, 0xD4),
            Hover = Color.FromRgb(0x22, 0x18, 0x3D),
            Pressed = Color.FromRgb(0x2A, 0x1C, 0x49),
            Input = Color.FromRgb(0x14, 0x0F, 0x25),
            Header = Color.FromRgb(0x1A, 0x14, 0x30),
            SubtleBorder = Color.FromRgb(0x35, 0x28, 0x55),
            DataGridAltRow = Color.FromRgb(0x1D, 0x15, 0x32),
            DataGridHeader = Color.FromRgb(0x1A, 0x14, 0x30),
            DataGridHorizontalLine = Color.FromRgb(0x30, 0x25, 0x4D),
            DataGridVerticalLine = Color.FromRgb(0x33, 0x27, 0x4F),
            DataGridHeaderSelected = Color.FromRgb(0x2A, 0x1C, 0x49),
            RequestChangesBadgeBackground = Color.FromRgb(0x3B, 0x18, 0x33),
            RequestChangesBadgeBorder = Color.FromRgb(0xFF, 0x5D, 0xA2),
            RequestChangesBadgeText = Color.FromRgb(0xFF, 0xC7, 0xE1),
            ApprovalBadgeBackground = Color.FromRgb(0x1A, 0x2E, 0x3A),
            ApprovalBadgeBorder = Color.FromRgb(0x00, 0xF5, 0xD4),
            ActivityBadgeBackground = Color.FromRgb(0x24, 0x1A, 0x39),
            ActivityBadgeBorder = Color.FromRgb(0x4C, 0x3E, 0x73),
            ActivityBadgeText = Color.FromRgb(0xD4, 0xCB, 0xFF)
        },
        AppThemeMode.DeepSea => _darkPalette with
        {
            Bg = Color.FromRgb(0x03, 0x18, 0x25),
            Panel = Color.FromRgb(0x08, 0x28, 0x39),
            PanelAlt = Color.FromRgb(0x0D, 0x3B, 0x4F),
            Header = Color.FromArgb(0xE6, 0x08, 0x2F, 0x42),
            Border = Color.FromRgb(0x24, 0x5E, 0x70),
            Accent = Color.FromRgb(0x54, 0xD6, 0xC8),
            Link = Color.FromRgb(0x96, 0xF0, 0xFF),
            Danger = Color.FromRgb(0xF0, 0x94, 0x76),
            Success = Color.FromRgb(0x8E, 0xD6, 0xB2),
            DataGridHeaderSelected = Color.FromRgb(0x14, 0x55, 0x67),
            ApprovalBadgeBackground = Color.FromRgb(0x16, 0x3D, 0x34),
            ActivityBadgeBackground = Color.FromRgb(0x13, 0x36, 0x56),
            ActivityBadgeText = Color.FromRgb(0xB9, 0xE6, 0xFF)
        },
        AppThemeMode.AlpineDawn => _darkPalette with
        {
            Bg = Color.FromRgb(0x09, 0x17, 0x2C),
            Panel = Color.FromRgb(0x10, 0x17, 0x27),
            PanelAlt = Color.FromRgb(0x13, 0x1B, 0x2D),
            Header = Color.FromArgb(0xF2, 0x11, 0x1A, 0x2C),
            Border = Color.FromRgb(0x53, 0x6C, 0x8A),
            Text = Color.FromRgb(0xF6, 0xF8, 0xFF),
            Muted = Color.FromRgb(0xB2, 0xBD, 0xD3),
            Accent = Color.FromRgb(0xF0, 0x8D, 0x83),
            Link = Color.FromRgb(0x90, 0xB6, 0xFF),
            Danger = Color.FromRgb(0xF0, 0x8D, 0x83),
            Success = Color.FromRgb(0xB5, 0xDE, 0xC9),
            Hover = Color.FromRgb(0x2A, 0x3D, 0x57),
            Pressed = Color.FromRgb(0x34, 0x4A, 0x68),
            Input = Color.FromRgb(0x13, 0x15, 0x1B),
            SubtleBorder = Color.FromArgb(0x64, 0x53, 0x6C, 0x8A),
            DataGridAltRow = Color.FromRgb(0x13, 0x20, 0x33),
            DataGridHeader = Color.FromRgb(0x16, 0x20, 0x36),
            DataGridHorizontalLine = Color.FromRgb(0x24, 0x2F, 0x43),
            DataGridVerticalLine = Color.FromRgb(0x2A, 0x35, 0x4A),
            DataGridHeaderSelected = Color.FromRgb(0x22, 0x30, 0x4A),
            RequestChangesBadgeBackground = Color.FromRgb(0x36, 0x24, 0x2C),
            RequestChangesBadgeBorder = Color.FromRgb(0x79, 0x67, 0x7E),
            RequestChangesBadgeText = Color.FromRgb(0xF4, 0xB6, 0xA6),
            ApprovalBadgeBackground = Color.FromRgb(0x1C, 0x33, 0x32),
            ApprovalBadgeBorder = Color.FromRgb(0x56, 0x78, 0x76),
            ActivityBadgeBackground = Color.FromRgb(0x22, 0x2B, 0x46),
            ActivityBadgeBorder = Color.FromRgb(0x6F, 0x7E, 0x9E),
            ActivityBadgeText = Color.FromRgb(0xC5, 0xD2, 0xEA)
        },
        AppThemeMode.Dark => _darkPalette,
        _ => _darkPalette
    };

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
        Resources["DataGridHeaderBrush"] = SystemColors.ControlBrush;
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
            ApplyTheme(viewModel.ThemeMode);
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

    private void OnDataGridPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not DependencyObject source)
        {
            return;
        }

        var scrollViewer = FindDescendant<ScrollViewer>(source);
        if (scrollViewer is null)
        {
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            ScrollHorizontally(scrollViewer, e);
            return;
        }

        ScrollVertically(scrollViewer, e);
    }

    private void ScrollHorizontally(ScrollViewer scrollViewer, MouseWheelEventArgs e)
    {
        if (scrollViewer.ScrollableWidth <= 0)
        {
            return;
        }

        scrollViewer.ScrollToHorizontalOffset(
            scrollViewer.HorizontalOffset - (e.Delta * _horizontalScrollWheelMultiplier));
        e.Handled = true;
    }

    private void ScrollVertically(ScrollViewer scrollViewer, MouseWheelEventArgs e)
    {
        if (scrollViewer.ScrollableHeight <= 0)
        {
            return;
        }

        var wheelScrollLines = SystemParameters.WheelScrollLines > 0
            ? SystemParameters.WheelScrollLines
            : DEFAULT_MOUSE_WHEEL_SCROLL_LINES;
        var scrollDelta = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine
            * wheelScrollLines
            * _verticalScrollWheelMultiplier;

        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - scrollDelta);
        e.Handled = true;
    }

    private static double ReadScrollWheelMultiplier(
        IConfiguration configuration,
        string key,
        double defaultMultiplier)
    {
        var configuredValue = configuration[key];
        if (double.TryParse(
                configuredValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var multiplier)
            && double.IsFinite(multiplier)
            && multiplier > 0)
        {
            return multiplier;
        }

        return defaultMultiplier;
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

    private static void FitDataGridColumns(DataGrid dataGrid)
    {
        dataGrid.UpdateLayout();
        var visibleColumns = dataGrid.Columns
            .Where(static column => column.Visibility == Visibility.Visible)
            .ToArray();

        if (visibleColumns.Length == 0)
        {
            return;
        }

        var availableWidth = GetAvailableDataGridWidth(dataGrid);
        if (availableWidth <= 0)
        {
            return;
        }

        var columnWeights = visibleColumns
            .Select(GetColumnWeight)
            .ToArray();
        var totalWeight = columnWeights.Sum();
        if (totalWeight <= 0)
        {
            return;
        }

        const double minimumColumnWidth = 44;
        const double fitPadding = 4;
        var fittedWidth = Math.Max(visibleColumns.Length * minimumColumnWidth, availableWidth - fitPadding);

        for (var index = 0; index < visibleColumns.Length; index++)
        {
            var column = visibleColumns[index];
            var width = fittedWidth * columnWeights[index] / totalWeight;
            column.MinWidth = Math.Min(column.MinWidth <= 0 ? minimumColumnWidth : column.MinWidth, minimumColumnWidth);
            column.Width = new DataGridLength(Math.Max(minimumColumnWidth, width), DataGridLengthUnitType.Pixel);
        }
    }

    private static double GetColumnWeight(DataGridColumn column)
    {
        if (column.ActualWidth > 0)
        {
            return column.ActualWidth;
        }

        return column.Width.Value > 0
            ? column.Width.Value
            : 1;
    }

    private static double GetAvailableDataGridWidth(DataGrid dataGrid)
    {
        var scrollViewer = FindDescendant<ScrollViewer>(dataGrid);
        if (scrollViewer?.ViewportWidth > 0)
        {
            return scrollViewer.ViewportWidth;
        }

        return Math.Max(0, dataGrid.ActualWidth - SystemParameters.VerticalScrollBarWidth - 2);
    }

    private static T? FindDescendant<T>(DependencyObject parent)
        where T : DependencyObject
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                return match;
            }

            var descendant = FindDescendant<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
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

    private const double DEFAULT_SCROLL_WHEEL_MULTIPLIER = 1.0;
    private const int DEFAULT_MOUSE_WHEEL_SCROLL_LINES = 3;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private static readonly ThemePalette _lightPalette = new(
        Bg: Color.FromRgb(0xEF, 0xEF, 0xEF),
        Panel: Color.FromRgb(0xF7, 0xF7, 0xF7),
        PanelAlt: Color.FromRgb(0xDD, 0xDD, 0xDD),
        Border: Color.FromRgb(0xC9, 0xC9, 0xC9),
        Text: Color.FromRgb(0x00, 0x00, 0x00),
        Muted: Color.FromRgb(0x63, 0x63, 0x63),
        Accent: Color.FromRgb(0x00, 0x52, 0xCC),
        Link: Color.FromRgb(0x00, 0x4A, 0xB8),
        Danger: Color.FromRgb(0xD0, 0x00, 0x00),
        Success: Color.FromRgb(0x17, 0x78, 0x00),
        Hover: Color.FromRgb(0xE7, 0xE7, 0xE7),
        Pressed: Color.FromRgb(0xD2, 0xD2, 0xD2),
        Input: Color.FromRgb(0xFC, 0xFC, 0xFC),
        Header: Color.FromRgb(0xD8, 0xD8, 0xD8),
        SubtleBorder: Color.FromRgb(0xC4, 0xC4, 0xC4),
        DataGridAltRow: Color.FromRgb(0xF0, 0xF0, 0xF0),
        DataGridHeader: Color.FromRgb(0xDD, 0xDD, 0xDD),
        DataGridHorizontalLine: Color.FromRgb(0xD7, 0xD7, 0xD7),
        DataGridVerticalLine: Color.FromRgb(0xE1, 0xE1, 0xE1),
        DataGridHeaderSelected: Color.FromRgb(0xC9, 0xDD, 0xF2),
        DisabledButton: Color.FromRgb(0xE3, 0xE3, 0xE3),
        DisabledText: Color.FromRgb(0x88, 0x88, 0x88),
        PrimaryButton: Color.FromRgb(0x00, 0x65, 0xB8),
        PrimaryButtonBorder: Color.FromRgb(0x00, 0x4E, 0x8C),
        ToggleTrack: Color.FromRgb(0xBC, 0xBC, 0xBC),
        ToggleThumb: Color.FromRgb(0xFF, 0xFF, 0xFF),
        ToggleCheckedTrack: Color.FromRgb(0x00, 0x65, 0xB8),
        RequestChangesBadgeBackground: Color.FromRgb(0xFF, 0xE8, 0xE3),
        RequestChangesBadgeBorder: Color.FromRgb(0xE6, 0xA0, 0x91),
        RequestChangesBadgeText: Color.FromRgb(0x9A, 0x2F, 0x18),
        ApprovalBadgeBackground: Color.FromRgb(0xE4, 0xF4, 0xE7),
        ApprovalBadgeBorder: Color.FromRgb(0x91, 0xC9, 0x9B),
        ActivityBadgeBackground: Color.FromRgb(0xF0, 0xE8, 0xFA),
        ActivityBadgeBorder: Color.FromRgb(0xBA, 0xA2, 0xD4),
        ActivityBadgeText: Color.FromRgb(0x63, 0x3C, 0x85));

    private static readonly ThemePalette _darkPalette = new(
        Bg: Color.FromRgb(0x1E, 0x1E, 0x1E),
        Panel: Color.FromRgb(0x25, 0x25, 0x26),
        PanelAlt: Color.FromRgb(0x2D, 0x2D, 0x30),
        Border: Color.FromRgb(0x3C, 0x3C, 0x3C),
        Text: Color.FromRgb(0xCC, 0xCC, 0xCC),
        Muted: Color.FromRgb(0x8B, 0x8B, 0x8B),
        Accent: Color.FromRgb(0x37, 0x94, 0xFF),
        Link: Color.FromRgb(0x4F, 0xC1, 0xFF),
        Danger: Color.FromRgb(0xF4, 0x87, 0x71),
        Success: Color.FromRgb(0x89, 0xD1, 0x85),
        Hover: Color.FromRgb(0x34, 0x34, 0x38),
        Pressed: Color.FromRgb(0x3F, 0x3F, 0x46),
        Input: Color.FromRgb(0x1F, 0x1F, 0x1F),
        Header: Color.FromArgb(0xDD, 0x25, 0x25, 0x26),
        SubtleBorder: Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF),
        DataGridAltRow: Color.FromRgb(0x28, 0x28, 0x2B),
        DataGridHeader: Color.FromRgb(0x2D, 0x2D, 0x30),
        DataGridHorizontalLine: Color.FromRgb(0x2D, 0x2D, 0x30),
        DataGridVerticalLine: Color.FromRgb(0x25, 0x25, 0x26),
        DataGridHeaderSelected: Color.FromRgb(0x17, 0x3B, 0x57),
        DisabledButton: Color.FromRgb(0x20, 0x20, 0x24),
        DisabledText: Color.FromRgb(0x77, 0x77, 0x77),
        PrimaryButton: Color.FromRgb(0x0E, 0x63, 0x9C),
        PrimaryButtonBorder: Color.FromRgb(0x11, 0x77, 0xBB),
        ToggleTrack: Color.FromRgb(0x3C, 0x3C, 0x3C),
        ToggleThumb: Color.FromRgb(0xCC, 0xCC, 0xCC),
        ToggleCheckedTrack: Color.FromRgb(0x37, 0x94, 0xFF),
        RequestChangesBadgeBackground: Color.FromRgb(0x32, 0x22, 0x1F),
        RequestChangesBadgeBorder: Color.FromRgb(0x5A, 0x2D, 0x26),
        RequestChangesBadgeText: Color.FromRgb(0xFF, 0x8A, 0x65),
        ApprovalBadgeBackground: Color.FromRgb(0x1D, 0x33, 0x24),
        ApprovalBadgeBorder: Color.FromRgb(0x2F, 0x5F, 0x3B),
        ActivityBadgeBackground: Color.FromRgb(0x2A, 0x26, 0x36),
        ActivityBadgeBorder: Color.FromRgb(0x4F, 0x45, 0x68),
        ActivityBadgeText: Color.FromRgb(0xD7, 0xBA, 0xFF));

    private sealed record ThemePalette(
        Color Bg,
        Color Panel,
        Color PanelAlt,
        Color Border,
        Color Text,
        Color Muted,
        Color Accent,
        Color Link,
        Color Danger,
        Color Success,
        Color Hover,
        Color Pressed,
        Color Input,
        Color Header,
        Color SubtleBorder,
        Color DataGridAltRow,
        Color DataGridHeader,
        Color DataGridHorizontalLine,
        Color DataGridVerticalLine,
        Color DataGridHeaderSelected,
        Color DisabledButton,
        Color DisabledText,
        Color PrimaryButton,
        Color PrimaryButtonBorder,
        Color ToggleTrack,
        Color ToggleThumb,
        Color ToggleCheckedTrack,
        Color RequestChangesBadgeBackground,
        Color RequestChangesBadgeBorder,
        Color RequestChangesBadgeText,
        Color ApprovalBadgeBackground,
        Color ApprovalBadgeBorder,
        Color ActivityBadgeBackground,
        Color ActivityBadgeBorder,
        Color ActivityBadgeText);

    private readonly MainViewModel _viewModel;
    private readonly double _horizontalScrollWheelMultiplier;
    private readonly double _verticalScrollWheelMultiplier;

    private int _dialogOverlayScopes;
}
