using System.Collections;
using System.ComponentModel;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Stores validation errors for a view model implementing <see cref="INotifyDataErrorInfo"/>.
/// </summary>
internal sealed class ValidationErrorStore
{
    /// <summary>
    /// Gets a value indicating whether any validation errors are currently stored.
    /// </summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Raised when validation errors change for a property.
    /// </summary>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <summary>
    /// Gets errors for a property, or all errors when no property name is supplied.
    /// </summary>
    /// <param name="propertyName">Property name to get errors for.</param>
    /// <returns>Stored validation errors.</returns>
    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return _errors.Values.SelectMany(static errors => errors);
        }

        return _errors.TryGetValue(propertyName, out var errors)
            ? errors
            : [];
    }

    /// <summary>
    /// Stores one validation error for a property.
    /// </summary>
    /// <param name="propertyName">Property name.</param>
    /// <param name="error">User-facing validation error.</param>
    public void SetError(string propertyName, string error)
    {
        _errors[propertyName] = [error];
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Clears validation errors for a property.
    /// </summary>
    /// <param name="propertyName">Property name.</param>
    /// <returns><see langword="true"/> when errors were removed.</returns>
    public bool ClearError(string propertyName)
    {
        if (!_errors.Remove(propertyName))
        {
            return false;
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        return true;
    }

    private readonly Dictionary<string, string[]> _errors = new(StringComparer.Ordinal);
}
