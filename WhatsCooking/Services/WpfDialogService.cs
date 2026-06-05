using System.Diagnostics.CodeAnalysis;
using System.Windows;

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
        var result = MessageBox.Show(
            "Pull requests are already loaded. Reload data from Bitbucket?",
            "Reload data",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        return result == MessageBoxResult.Yes;
    }

    /// <inheritdoc />
    public void ShowLoadError(string message)
    {
        _ = MessageBox.Show(message, "Load failed", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
