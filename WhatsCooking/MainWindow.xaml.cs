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
        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var useDarkMode = 1;

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
