using System.Windows.Controls;

using FluentAssertions;

using WhatsCooking.Views;

namespace WhatsCooking.Tests;

public sealed class WpfViewsTests
{
    [Fact(DisplayName = "Dashboard toolbar loads XAML content")]
    [Trait("Category", "Unit")]
    public void DashboardToolbarConstructorWhenCalledLoadsContent()
    {
        StaTest.Run(() =>
        {
            // Act
            var view = new DashboardToolbar();

            // Assert
            view.Content.Should().NotBeNull();
            view.FindName("CopyPullRequestsButton").Should().BeOfType<Button>()
                .Which.Content.Should().BeOfType<StackPanel>();
            view.FindName("ResetFiltersButton").Should().BeOfType<Button>()
                .Which.Content.Should().BeOfType<StackPanel>();
        });
    }

    [Fact(DisplayName = "PR copy selection uses displayed rows unless rows are selected")]
    [Trait("Category", "Unit")]
    public void SelectDisplayedItemsForCopyWhenSelectionChangesResolvesExpectedRows()
    {
        // Arrange
        object[] displayedItems = ["first", "second", "third"];

        // Act
        var withoutSelection = MainWindow.SelectDisplayedItemsForCopy<string>(displayedItems, Array.Empty<object>());
        var withSelection = MainWindow.SelectDisplayedItemsForCopy<string>(displayedItems, new object[] { "third", "first" });

        // Assert
        withoutSelection.Should().Equal("first", "second", "third");
        withSelection.Should().Equal("first", "third");
    }

    [Fact(DisplayName = "Loading overlay loads XAML content")]
    [Trait("Category", "Unit")]
    public void LoadingOverlayConstructorWhenCalledLoadsContent()
    {
        StaTest.Run(() =>
        {
            // Act
            var view = new LoadingOverlay();

            // Assert
            view.Content.Should().NotBeNull();
        });
    }

    [Fact(DisplayName = "Telemetry view loads XAML content")]
    [Trait("Category", "Unit")]
    public void TelemetryViewConstructorWhenCalledLoadsContent()
    {
        StaTest.Run(() =>
        {
            // Act
            var view = new TelemetryView();

            // Assert
            view.Content.Should().NotBeNull();
        });
    }

    [Fact(DisplayName = "Styled dialog window loads XAML content")]
    [Trait("Category", "Unit")]
    public void StyledDialogWindowConstructorWhenCalledLoadsContent()
    {
        StaTest.Run(() =>
        {
            // Act
            var window = new StyledDialogWindow();

            // Assert
            window.Content.Should().NotBeNull();
            window.DialogTitle.Should().BeEmpty();
            window.Message.Should().BeEmpty();
        });
    }
}
