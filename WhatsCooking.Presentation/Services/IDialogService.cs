namespace WhatsCooking.Services;

/// <summary>
/// Shows user-facing dialogs.
/// </summary>
internal interface IDialogService
{
    /// <summary>
    /// Asks whether already loaded pull request data should be reloaded.
    /// </summary>
    /// <returns><see langword="true"/> when the user confirms reload.</returns>
    bool ConfirmReload();

    /// <summary>
    /// Asks whether persisted pull request details cache should be cleared.
    /// </summary>
    /// <returns><see langword="true"/> when the user confirms cache clearing.</returns>
    bool ConfirmClearCache();

    /// <summary>
    /// Shows a load failure message.
    /// </summary>
    /// <param name="message">Error message to show.</param>
    void ShowLoadError(string message);
}
