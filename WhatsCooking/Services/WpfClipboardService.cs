using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace WhatsCooking.Services;

/// <summary>
/// Writes text to the Windows clipboard.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Service is created by dependency injection.")]
internal sealed class WpfClipboardService : IClipboardService
{
    /// <inheritdoc />
    public void SetText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Clipboard.SetText(text);
    }
}
