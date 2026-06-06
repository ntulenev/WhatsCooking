using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;

namespace WhatsCooking.Views;

/// <summary>
/// Bitbucket API telemetry dashboard.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Control is created by XAML.")]
internal sealed partial class TelemetryView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryView"/> class.
    /// </summary>
    public TelemetryView()
    {
        InitializeComponent();
    }
}
