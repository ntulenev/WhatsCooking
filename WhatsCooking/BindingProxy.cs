using System.Windows;
using System.Diagnostics.CodeAnalysis;

namespace WhatsCooking;

/// <summary>
/// Exposes inherited data context to objects outside the WPF visual tree.
/// </summary>
internal sealed class BindingProxy : Freezable
{
    /// <summary>
    /// Gets or sets the proxied data context.
    /// </summary>
    public object? Data {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="Data"/> dependency property.
    /// </summary>
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "WPF dependency property fields follow the PropertyNameProperty convention.")]
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data),
        typeof(object),
        typeof(BindingProxy));

    /// <inheritdoc />
    protected override Freezable CreateInstanceCore() => new BindingProxy();
}
