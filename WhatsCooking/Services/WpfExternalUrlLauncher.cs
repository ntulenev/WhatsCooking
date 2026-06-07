using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace WhatsCooking.Services;

/// <summary>
/// Opens external URLs through the Windows shell.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Service is created by dependency injection.")]
internal sealed class WpfExternalUrlLauncher : IExternalUrlLauncher
{
    /// <inheritdoc />
    public void Open(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);

        _ = Process.Start(new ProcessStartInfo(url.ToString())
        {
            UseShellExecute = true
        });
    }
}
