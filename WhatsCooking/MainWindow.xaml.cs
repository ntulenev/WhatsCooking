using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
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
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApplyTheme(viewModel.IsLightTheme);
        SourceInitialized += OnSourceInitialized;
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

    private void ApplyTheme(bool isLightTheme)
    {
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
        }

        Background = (Brush)FindResource("BgBrush");
        ApplyWindowFrameTheme(isLightTheme);
    }

    private void SetBrushColor(string resourceKey, Color color) => Resources[resourceKey] = new SolidColorBrush(color);

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

    private void OnColumnHeaderPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not DataGridColumnHeader header
            || IsFromFilterTextBox(e.OriginalSource)
            || IsFromColumnResizeGrip(e.OriginalSource))
        {
            return;
        }

        if (header.Column is not { SortMemberPath: { } sortMemberPath } sortColumn
            || string.IsNullOrWhiteSpace(sortMemberPath))
        {
            return;
        }

        var dataGrid = FindVisualParent<DataGrid>(header);
        if (dataGrid?.ItemsSource is not ICollectionView view)
        {
            return;
        }

        var direction = sortColumn.SortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(sortMemberPath, direction));
        view.Refresh();

        foreach (var dataGridColumn in dataGrid.Columns)
        {
            dataGridColumn.SortDirection = null;
        }

        sortColumn.SortDirection = direction;
        e.Handled = true;
    }

    private void OnHeaderFilterTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox { Tag: string tag } textBox || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var parts = tag.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return;
        }

        viewModel.ApplyPullRequestFilter(parts[0], parts[1], textBox.Text);
    }

    private static bool IsFromFilterTextBox(object source) => FindVisualParent<TextBox>(source as DependencyObject) is not null;

    private static bool IsFromColumnResizeGrip(object source) => FindVisualParent<Thumb>(source as DependencyObject) is not null;

    private static T? FindVisualParent<T>(DependencyObject? source)
        where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T parent)
            {
                return parent;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    [LibraryImport("dwmapi.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
}
