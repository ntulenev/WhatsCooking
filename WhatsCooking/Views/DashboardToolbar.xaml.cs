using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;

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
}
