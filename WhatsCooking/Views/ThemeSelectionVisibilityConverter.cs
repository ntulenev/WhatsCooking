using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WhatsCooking.Views;

/// <summary>
/// Converts a theme option/current theme pair into a selection marker visibility.
/// </summary>
internal sealed class ThemeSelectionVisibilityConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return Visibility.Collapsed;
        }

        return Equals(values[0], values[1]) ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
