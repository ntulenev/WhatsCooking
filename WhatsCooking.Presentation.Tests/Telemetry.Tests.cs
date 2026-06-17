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
            Mock.Of<IPullRequestDetailsCache>(),
            Mock.Of<IDialogService>(),
            new Mock<IDebouncer>(MockBehavior.Strict).Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Telemetry view model constructor throws when debouncer is null")]
    [Trait("Category", "Unit")]
    public void TelemetryViewModelConstructorWhenCacheIsNullThrowsArgumentNullException()
    {
        // Arrange
        var telemetryService = new Mock<IBitbucketTelemetryService>(MockBehavior.Strict).Object;
        IPullRequestDetailsCache cache = null!;

        // Act
        Action act = () => _ = new TelemetryViewModel(
            telemetryService,
            cache,
            Mock.Of<IDialogService>(),
            new Mock<IDebouncer>(MockBehavior.Strict).Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Telemetry view model constructor throws when dialog service is null")]
    [Trait("Category", "Unit")]
    public void TelemetryViewModelConstructorWhenDialogServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        var telemetryService = new Mock<IBitbucketTelemetryService>(MockBehavior.Strict).Object;
        var cache = new Mock<IPullRequestDetailsCache>(MockBehavior.Strict).Object;
        IDialogService dialogService = null!;

        // Act
        Action act = () => _ = new TelemetryViewModel(
            telemetryService,
            cache,
            dialogService,
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
        var cache = new Mock<IPullRequestDetailsCache>(MockBehavior.Strict).Object;
        IDebouncer debouncer = null!;

        // Act
        Action act = () => _ = new TelemetryViewModel(
            telemetryService,
            cache,
            Mock.Of<IDialogService>(),
            debouncer);

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
        var cache = new Mock<IPullRequestDetailsCache>(MockBehavior.Strict);
        cache.Setup(instance => instance.GetSizeInBytes()).Returns(1536);
        var debouncer = new Mock<IDebouncer>(MockBehavior.Strict);
        using var viewModel = new TelemetryViewModel(
            telemetryService.Object,
            cache.Object,
            Mock.Of<IDialogService>(),
            debouncer.Object);
        debouncer.Setup(instance => instance.Dispose());

        // Act
        var result = viewModel.RefreshTelemetry();

        // Assert
        result.Should().BeSameAs(snapshot);
        viewModel.IsTelemetryEnabled.Should().BeTrue();
        viewModel.TelemetryRequestsCount.Should().Be(5);
        viewModel.TelemetryEndpointsCount.Should().Be(2);
        viewModel.CacheHits.Should().Be(3);
        viewModel.CacheMisses.Should().Be(2);
        viewModel.CacheHitRate.Should().Be("60.0 %");
        viewModel.CacheSize.Should().Be("1.5 KB");
        viewModel.TelemetryView.Select(row => row.ApiName).Should().Equal("repositories", "user");
        calls.Should().Be(1);
        telemetryService.VerifyAll();
        cache.VerifyAll();
    }

    [Fact(DisplayName = "LoadTelemetry throws when snapshot is null")]
    [Trait("Category", "Unit")]
    public void LoadTelemetryWhenSnapshotIsNullThrowsArgumentNullException()
    {
        // Arrange
        var viewModel = new TelemetryViewModel(
            new Mock<IBitbucketTelemetryService>(MockBehavior.Strict).Object,
            Mock.Of<IPullRequestDetailsCache>(instance => instance.GetSizeInBytes() == 0),
            Mock.Of<IDialogService>(),
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
            Mock.Of<IPullRequestDetailsCache>(instance => instance.GetSizeInBytes() == 0),
            Mock.Of<IDialogService>(),
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

    [Fact(DisplayName = "Clear cache command clears cache after confirmation")]
    [Trait("Category", "Unit")]
    public void ClearCacheCommandWhenConfirmedClearsCacheAndRefreshesSize()
    {
        // Arrange
        var cache = new Mock<IPullRequestDetailsCache>(MockBehavior.Strict);
        cache.SetupSequence(instance => instance.GetSizeInBytes())
            .Returns(1024)
            .Returns(0);
        cache.Setup(instance => instance.Clear());
        var dialogService = new Mock<IDialogService>(MockBehavior.Strict);
        dialogService.Setup(instance => instance.ConfirmClearCache()).Returns(true);
        var viewModel = new TelemetryViewModel(
            new Mock<IBitbucketTelemetryService>(MockBehavior.Strict).Object,
            cache.Object,
            dialogService.Object,
            new Mock<IDebouncer>(MockBehavior.Strict).Object);

        // Act
        viewModel.ClearCacheCommand.Execute(null);

        // Assert
        viewModel.CacheSize.Should().Be("0 B");
        cache.VerifyAll();
        dialogService.VerifyAll();
    }

    [Fact(DisplayName = "Clear cache command does not clear cache without confirmation")]
    [Trait("Category", "Unit")]
    public void ClearCacheCommandWhenDeclinedDoesNotClearCache()
    {
        // Arrange
        var cache = new Mock<IPullRequestDetailsCache>(MockBehavior.Strict);
        cache.Setup(instance => instance.GetSizeInBytes()).Returns(1024);
        var dialogService = new Mock<IDialogService>(MockBehavior.Strict);
        dialogService.Setup(instance => instance.ConfirmClearCache()).Returns(false);
        var viewModel = new TelemetryViewModel(
            new Mock<IBitbucketTelemetryService>(MockBehavior.Strict).Object,
            cache.Object,
            dialogService.Object,
            new Mock<IDebouncer>(MockBehavior.Strict).Object);

        // Act
        viewModel.ClearCacheCommand.Execute(null);

        // Assert
        viewModel.CacheSize.Should().Be("1.0 KB");
        cache.Verify(instance => instance.Clear(), Times.Never);
        cache.VerifyAll();
        dialogService.VerifyAll();
    }

    private static BitbucketTelemetrySnapshot CreateSnapshot() =>
        new(
            true,
            5,
            [
                new BitbucketApiRequestStatistic("repositories", 4),
                new BitbucketApiRequestStatistic("user", 1)
            ],
            CacheHits: 3,
            CacheMisses: 2);
}
