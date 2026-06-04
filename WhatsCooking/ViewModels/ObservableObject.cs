using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Base class for view models that expose property change notifications.
/// </summary>
internal abstract class ObservableObject : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">Name of the changed property.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Updates a backing field and raises a property change notification when needed.
    /// </summary>
    /// <typeparam name="T">Property value type.</typeparam>
    /// <param name="field">Backing field reference.</param>
    /// <param name="value">New property value.</param>
    /// <param name="propertyName">Name of the changed property.</param>
    /// <returns><see langword="true"/> when the value has changed.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
