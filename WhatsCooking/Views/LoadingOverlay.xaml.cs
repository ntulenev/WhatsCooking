using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;

namespace WhatsCooking.Views;

/// <summary>
/// Modal loading progress surface.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Control is created by XAML.")]
internal sealed partial class LoadingOverlay : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadingOverlay"/> class.
    /// </summary>
    public LoadingOverlay()
    {
        InitializeComponent();
    }
}
