namespace WhatsCooking.Services;

/// <summary>
/// Opens external URLs using the host platform.
/// </summary>
internal interface IExternalUrlLauncher
{
    /// <summary>
    /// Opens the specified URL.
    /// </summary>
    /// <param name="url">URL to open.</param>
    void Open(Uri url);
}
