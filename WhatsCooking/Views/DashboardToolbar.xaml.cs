using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Views;

/// <summary>
/// Dashboard search, loading, and appearance controls.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Control is created by XAML.")]
internal sealed partial class DashboardToolbar : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardToolbar"/> class.
    /// </summary>
    public DashboardToolbar()
    {
        InitializeComponent();
    }

    private void OnFitColumnsClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.FitCurrentDataGridColumns();
        }
    }

    private void OnToggleHeaderMenuClick(object sender, RoutedEventArgs e)
    {
        HeaderMenuPopup.IsOpen = !HeaderMenuPopup.IsOpen;
        if (!HeaderMenuPopup.IsOpen)
        {
            CollapseThemeMenu();
        }
    }

    private void OnHeaderMenuClosed(object? sender, EventArgs e) => CollapseThemeMenu();

    private void OnToggleThemeMenuClick(object sender, RoutedEventArgs e)
    {
        ThemeMenuItemsPanel.Visibility = ThemeMenuItemsPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void OnThemeModeMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel
            || sender is not FrameworkElement { DataContext: AppThemeOption option })
        {
            return;
        }

        viewModel.ThemeMode = option.Mode;
        CloseHeaderMenu();
    }

    private void CloseHeaderMenu()
    {
        HeaderMenuPopup.IsOpen = false;
        CollapseThemeMenu();
    }

    private void CollapseThemeMenu() => ThemeMenuItemsPanel.Visibility = Visibility.Collapsed;
}
