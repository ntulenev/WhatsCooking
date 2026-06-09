using System.Globalization;

using BBRepoList.Abstractions;
using BBRepoList.Models;

using FluentAssertions;

using Moq;

using WhatsCooking.Services;
using WhatsCooking.ViewModels;

namespace WhatsCooking.Presentation.Tests;

public sealed class TelemetryTests
{
    [Fact(DisplayName = "Telemetry row constructor throws when statistic is null")]
    [Trait("Category", "Unit")]
    public void TelemetryRowConstructorWhenStatisticIsNullThrowsArgumentNullException()
    {
        // Arrange
        BitbucketApiRequestStatistic statistic = null!;

        // Act
        Action act = () => _ = new TelemetryRow(1, statistic, 10);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory(DisplayName = "Telemetry row calculates request share")]
    [Trait("Category", "Unit")]
    [InlineData(2, 8, 0.25, "25.0 %")]
    [InlineData(2, 0, 0, "0.0 %")]
    public void TelemetryRowConstructorWhenCalledCalculatesRequestShare(
        int requestCount,
        int totalRequests,
        double expectedShare,
        string expectedShareText)
    {
        // Arrange
        var statistic = new BitbucketApiRequestStatistic("repositories", requestCount);

        // Act
        var row = new TelemetryRow(3, statistic, totalRequests);

        // Assert
        row.Should().BeEquivalentTo(new
        {
            Number = 3,
            ApiName = "repositories",
            RequestCount = requestCount,
            Share = expectedShare,
            ShareText = expectedShareText
        });
        row.SearchText.Should().ContainAll(
            "3",
            "repositories",
            requestCount.ToString(CultureInfo.InvariantCulture),
            expectedShareText);
    }

    [Fact(DisplayName = "Telemetry view model constructor throws when telemetry service is null")]
    [Trait("Category", "Unit")]
    public void TelemetryViewModelConstructorWhenTelemetryServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketTelemetryService telemetryService = null!;

        // Act
        Action act = () => _ = new TelemetryViewModel(
            telemetryService,
            new Mock<IDebouncer>(MockBehavior.Strict).Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Telemetry view model constructor throws when debouncer is null")]
    [Trait("Category", "Unit")]
    public void TelemetryViewModelConstructorWhenDebouncerIsNullThrowsArgumentNullException()
    {
        // Arrange
        var telemetryService = new Mock<IBitbucketTelemetryService>(MockBehavior.Strict).Object;
        IDebouncer debouncer = null!;

        // Act
        Action act = () => _ = new TelemetryViewModel(telemetryService, debouncer);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "RefreshTelemetry loads exact service snapshot")]
    [Trait("Category", "Unit")]
    public void RefreshTelemetryWhenCalledLoadsExactServiceSnapshot()
    {
        // Arrange
        var snapshot = CreateSnapshot();
        var calls = 0;
        var telemetryService = new Mock<IBitbucketTelemetryService>(MockBehavior.Strict);
        telemetryService.Setup(instance => instance.GetSnapshot())
            .Callback(() => calls++)
            .Returns(snapshot);
        var debouncer = new Mock<IDebouncer>(MockBehavior.Strict);
        using var viewModel = new TelemetryViewModel(telemetryService.Object, debouncer.Object);
        debouncer.Setup(instance => instance.Dispose());

        // Act
        var result = viewModel.RefreshTelemetry();

        // Assert
        result.Should().BeSameAs(snapshot);
        viewModel.IsTelemetryEnabled.Should().BeTrue();
        viewModel.TelemetryRequestsCount.Should().Be(5);
        viewModel.TelemetryEndpointsCount.Should().Be(2);
        viewModel.TelemetryView.Select(row => row.ApiName).Should().Equal("repositories", "user");
        calls.Should().Be(1);
        telemetryService.VerifyAll();
    }

    [Fact(DisplayName = "LoadTelemetry throws when snapshot is null")]
    [Trait("Category", "Unit")]
    public void LoadTelemetryWhenSnapshotIsNullThrowsArgumentNullException()
    {
        // Arrange
        var viewModel = new TelemetryViewModel(
            new Mock<IBitbucketTelemetryService>(MockBehavior.Strict).Object,
            new Mock<IDebouncer>(MockBehavior.Strict).Object);
        BitbucketTelemetrySnapshot snapshot = null!;

        // Act
        Action act = () => viewModel.LoadTelemetry(snapshot);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Telemetry filter schedules exact refresh and filters rows")]
    [Trait("Category", "Unit")]
    public void TelemetryFilterWhenChangedSchedulesRefreshAndFiltersRows()
    {
        // Arrange
        Action? scheduledAction = null;
        var scheduleCalls = 0;
        var debouncer = new Mock<IDebouncer>(MockBehavior.Strict);
        debouncer.Setup(instance => instance.Schedule(
                It.IsAny<Action>(),
                TimeSpan.FromMilliseconds(150)))
            .Callback<Action, TimeSpan>((action, _) =>
            {
                scheduleCalls++;
                scheduledAction = action;
            });
        debouncer.Setup(instance => instance.Dispose());
        var viewModel = new TelemetryViewModel(
            new Mock<IBitbucketTelemetryService>(MockBehavior.Strict).Object,
            debouncer.Object);
        viewModel.LoadTelemetry(CreateSnapshot());

        // Act
        viewModel.TelemetryFilter = "USER";
        scheduledAction!();

        // Assert
        viewModel.TelemetryView.Should().ContainSingle()
            .Which.ApiName.Should().Be("user");
        scheduleCalls.Should().Be(1);
        viewModel.Dispose();
        debouncer.VerifyAll();
    }

    private static BitbucketTelemetrySnapshot CreateSnapshot() =>
        new(
            true,
            5,
            [
                new BitbucketApiRequestStatistic("repositories", 4),
                new BitbucketApiRequestStatistic("user", 1)
            ]);
}
