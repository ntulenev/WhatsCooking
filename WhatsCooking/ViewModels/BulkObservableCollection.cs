using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Observable collection that can replace its contents with one reset notification.
/// </summary>
/// <typeparam name="T">Collection item type.</typeparam>
internal sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
    /// <summary>
    /// Replaces all collection items.
    /// </summary>
    /// <param name="items">New collection contents.</param>
    public void ReplaceAll(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
