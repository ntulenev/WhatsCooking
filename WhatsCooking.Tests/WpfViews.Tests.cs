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
        });
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
