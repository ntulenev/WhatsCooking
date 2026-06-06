using System.Collections.Specialized;

using WhatsCooking.ViewModels;

using Xunit;

namespace WhatsCooking.Tests;

public sealed class BulkObservableCollectionTests
{
    [Fact]
    public void ReplaceAllPublishesSingleResetNotification()
    {
        var collection = new BulkObservableCollection<int> { 1, 2 };
        var changes = new List<NotifyCollectionChangedEventArgs>();
        collection.CollectionChanged += (_, args) => changes.Add(args);

        collection.ReplaceAll([3, 4, 5]);

        Assert.Equal([3, 4, 5], collection);
        var change = Assert.Single(changes);
        Assert.Equal(NotifyCollectionChangedAction.Reset, change.Action);
    }
}
