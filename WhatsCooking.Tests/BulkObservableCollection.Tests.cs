using System.Collections.Specialized;

using FluentAssertions;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Tests;

public sealed class BulkObservableCollectionTests
{
    [Fact(DisplayName = "ReplaceAll throws when items are null")]
    [Trait("Category", "Unit")]
    public void ReplaceAllWhenItemsAreNullThrowsArgumentNullException()
    {
        // Arrange
        var collection = new BulkObservableCollection<int>();
        IEnumerable<int> items = null!;

        // Act
        Action act = () => collection.ReplaceAll(items);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ReplaceAll replaces items and publishes one reset notification")]
    [Trait("Category", "Unit")]
    public void ReplaceAllPublishesSingleResetNotification()
    {
        // Arrange
        var collection = new BulkObservableCollection<int> { 1, 2 };
        var changes = new List<NotifyCollectionChangedEventArgs>();
        collection.CollectionChanged += (_, args) => changes.Add(args);

        // Act
        collection.ReplaceAll([3, 4, 5]);

        // Assert
        collection.Should().Equal(3, 4, 5);
        changes.Should().ContainSingle()
            .Which.Action.Should().Be(NotifyCollectionChangedAction.Reset);
    }
}
