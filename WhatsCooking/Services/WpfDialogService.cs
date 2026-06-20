using System.Diagnostics.CodeAnalysis;
using System.Windows;

using WhatsCooking.Views;

namespace WhatsCooking.Services;

/// <summary>
/// WPF implementation of user-facing dialogs.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Service is created by dependency injection.")]
internal sealed class WpfDialogService : IDialogService
{
    /// <inheritdoc />
    public bool ConfirmReload()
    {
        return StyledDialogWindow.ShowConfirmation(
            Application.Current?.MainWindow,
            "Reload data",
            "Pull requests are already loaded. Reload data from Bitbucket?",
            "?");
    }

    /// <inheritdoc />
    public bool ConfirmClearCache()
    {
        return StyledDialogWindow.ShowConfirmation(
            Application.Current?.MainWindow,
            "Clear cache",
            "Clear the pull request details cache? Cached data will be downloaded again when needed.",
            "!");
    }

    /// <inheritdoc />
    public void ShowLoadError(string message)
    {
        StyledDialogWindow.ShowMessage(Application.Current?.MainWindow, "Load failed", message, "!");
    }

    /// <inheritdoc />
    public void ShowReloadSummary(string message)
    {
        StyledDialogWindow.ShowMessage(Application.Current?.MainWindow, "Pull request updates", message, "i");
    }
}
