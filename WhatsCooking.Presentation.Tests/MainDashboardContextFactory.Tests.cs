using FluentAssertions;

using Moq;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Presentation.Tests;

public sealed class MainDashboardContextFactoryTests
{
    [Fact(DisplayName = "Constructor throws when delegate is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenDelegateIsNullThrowsArgumentNullException()
    {
        // Act
        Action act = () => _ = new MainDashboardContextFactory(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Create delegates context creation and returns interface")]
    [Trait("Category", "Unit")]
    public void CreateDelegatesContextCreationAndReturnsInterface()
    {
        // Arrange
        var expectedContext = new Mock<IMainDashboardContext>(MockBehavior.Strict).Object;
        var telemetryDashboard = new Mock<ITelemetryDashboard>(MockBehavior.Strict).Object;
        Func<string> getGlobalSearch = static () => "search";
        Func<string>? capturedSearch = null;
        ITelemetryDashboard? capturedTelemetry = null;
        var factory = new MainDashboardContextFactory((search, telemetry) =>
        {
            capturedSearch = search;
            capturedTelemetry = telemetry;

            return expectedContext;
        });

        // Act
        var actualContext = factory.Create(getGlobalSearch, telemetryDashboard);

        // Assert
        actualContext.Should().BeSameAs(expectedContext);
        capturedSearch.Should().BeSameAs(getGlobalSearch);
        capturedTelemetry.Should().BeSameAs(telemetryDashboard);
    }

    [Theory(DisplayName = "Create throws when runtime dependency is null")]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(1)]
    public void CreateWhenRuntimeDependencyIsNullThrowsArgumentNullException(int dependencyIndex)
    {
        // Arrange
        var context = new Mock<IMainDashboardContext>(MockBehavior.Strict).Object;
        var factory = new MainDashboardContextFactory((_, _) => context);
        Func<string> getGlobalSearch = static () => string.Empty;
        var telemetryDashboard = new Mock<ITelemetryDashboard>(MockBehavior.Strict).Object;

        // Act
        Action act = () => _ = factory.Create(
            dependencyIndex == 0 ? null! : getGlobalSearch,
            dependencyIndex == 1 ? null! : telemetryDashboard);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
