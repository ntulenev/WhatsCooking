using FluentAssertions;

using WhatsCooking.Services;

namespace WhatsCooking.Tests;

public sealed class WpfExternalUrlLauncherTests
{
    [Fact(DisplayName = "Open throws when URL is null")]
    [Trait("Category", "Unit")]
    public void OpenWhenUrlIsNullThrowsArgumentNullException()
    {
        // Arrange
        var launcher = new WpfExternalUrlLauncher();
        Uri url = null!;

        // Act
        Action act = () => launcher.Open(url);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
