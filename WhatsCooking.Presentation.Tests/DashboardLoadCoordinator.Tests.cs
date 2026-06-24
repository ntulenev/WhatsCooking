using BBRepoList.Models;

using FluentAssertions;

using Moq;

using WhatsCooking.Services;

namespace WhatsCooking.Presentation.Tests;

public sealed class DashboardLoadCoordinatorTests
{
    [Fact(DisplayName = "Load returns success without reload summary for first load")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenFirstLoadSucceedsReturnsSnapshotWithoutReloadSummary()
    {
        // Arrange
        var snapshot = CreateSnapshot();
        var fixture = CreateFixture();
        fixture.LoadUseCase.Setup(instance => instance.LoadAsync(
                new FilterPattern("pay", RepositorySearchMode.Contains),
                7,
                It.IsAny<IProgress<PullRequestLoadProgress>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardLoadResult.Success(snapshot));
        var coordinator = fixture.CreateCoordinator();

        // Act
        var result = await coordinator.LoadAsync(
            new FilterPattern("pay", RepositorySearchMode.Contains),
            7,
            isReload: false,
            previousOpenPullRequests: [],
            previousMergedPullRequests: [],
            progress: null,
            CancellationToken.None);

        // Assert
        result.Should().Be(new DashboardLoadCoordinatorResult.Success(snapshot, ReloadSummary: null));
        fixture.DialogService.Verify(instance => instance.ConfirmReload(), Times.Never);
        fixture.ReloadSummaryService.Verify(
            instance => instance.CreateSummary(
                It.IsAny<IReadOnlyCollection<PullRequestDetail>>(),
                It.IsAny<IReadOnlyCollection<MergedPullRequest>>(),
                It.IsAny<PullRequestDashboardSnapshot>()),
            Times.Never);
        fixture.LoadUseCase.VerifyAll();
    }

    [Fact(DisplayName = "Load skips reload when user declines confirmation")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenReloadIsDeclinedReturnsSkipped()
    {
        // Arrange
        var fixture = CreateFixture();
        fixture.DialogService.Setup(instance => instance.ConfirmReload())
            .Returns(false);
        var coordinator = fixture.CreateCoordinator();

        // Act
        var result = await coordinator.LoadAsync(
            new FilterPattern("pay", RepositorySearchMode.Contains),
            7,
            isReload: true,
            previousOpenPullRequests: [],
            previousMergedPullRequests: [],
            progress: null,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<DashboardLoadCoordinatorResult.Skipped>();
        fixture.LoadUseCase.Verify(
            instance => instance.LoadAsync(
                It.IsAny<FilterPattern>(),
                It.IsAny<int>(),
                It.IsAny<IProgress<PullRequestLoadProgress>?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.DialogService.VerifyAll();
    }

    [Fact(DisplayName = "Load returns reload summary when confirmed reload succeeds")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenReloadSucceedsReturnsReloadSummary()
    {
        // Arrange
        var firstSnapshot = CreateSnapshot(10, 9);
        var nextSnapshot = CreateSnapshot(11, 8);
        var fixture = CreateFixture();
        fixture.DialogService.Setup(instance => instance.ConfirmReload())
            .Returns(true);
        fixture.LoadUseCase.Setup(instance => instance.LoadAsync(
                new FilterPattern("pay", RepositorySearchMode.Contains),
                7,
                It.IsAny<IProgress<PullRequestLoadProgress>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardLoadResult.Success(nextSnapshot));
        fixture.ReloadSummaryService.Setup(instance => instance.CreateSummary(
                firstSnapshot.OpenPullRequests,
                firstSnapshot.MergedPullRequests,
                nextSnapshot))
            .Returns("Reload summary.");
        var coordinator = fixture.CreateCoordinator();

        // Act
        var result = await coordinator.LoadAsync(
            new FilterPattern("pay", RepositorySearchMode.Contains),
            7,
            isReload: true,
            firstSnapshot.OpenPullRequests,
            firstSnapshot.MergedPullRequests,
            progress: null,
            CancellationToken.None);

        // Assert
        result.Should().Be(new DashboardLoadCoordinatorResult.Success(nextSnapshot, "Reload summary."));
        fixture.DialogService.VerifyAll();
        fixture.LoadUseCase.VerifyAll();
        fixture.ReloadSummaryService.VerifyAll();
    }

    [Fact(DisplayName = "Load maps use case failure and cancellation")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenUseCaseDoesNotSucceedMapsResult()
    {
        // Arrange
        var loadResults = new Queue<DashboardLoadResult>([
            new DashboardLoadResult.Failure("Could not load."),
            new DashboardLoadResult.Cancelled()
        ]);
        var fixture = CreateFixture();
        fixture.LoadUseCase.Setup(instance => instance.LoadAsync(
                new FilterPattern(),
                1,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(loadResults.Dequeue()));
        var coordinator = fixture.CreateCoordinator();

        // Act
        var failure = await coordinator.LoadAsync(new FilterPattern(), 1, false, [], [], null, CancellationToken.None);
        var cancelled = await coordinator.LoadAsync(new FilterPattern(), 1, false, [], [], null, CancellationToken.None);

        // Assert
        failure.Should().Be(new DashboardLoadCoordinatorResult.Failure("Could not load."));
        cancelled.Should().BeOfType<DashboardLoadCoordinatorResult.Cancelled>();
        fixture.LoadUseCase.VerifyAll();
    }

    private static PullRequestDashboardSnapshot CreateSnapshot(int openPullRequestId = 10, int mergedPullRequestId = 9)
    {
        var asOf = new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
        var repository = new Repository("Payments", slug: new RepositorySlug("payments"));

        return new PullRequestDashboardSnapshot(
            asOf,
            [repository],
            [
                new PullRequestDetail(
                    repository,
                    new PullRequestId(openPullRequestId),
                    $"Open title {openPullRequestId}",
                    asOf.AddHours(-2),
                    null,
                    "Nikita",
                    asOf.AddHours(-1),
                    asOf.AddMinutes(-30),
                    true)
            ],
            [
                new MergedPullRequest(
                    repository,
                    new PullRequestId(mergedPullRequestId),
                    $"Merged title {mergedPullRequestId}",
                    asOf.AddDays(-2),
                    null,
                    "Nikita",
                    asOf.AddDays(-1),
                    asOf.AddHours(-3),
                    false,
                    mergedOn: asOf.AddHours(-2))
            ],
            new BitbucketTelemetrySnapshot(true, 3, [new BitbucketApiRequestStatistic("user", 3)]));
    }

    private static DashboardLoadCoordinatorFixture CreateFixture() =>
        new(
            new Mock<IDashboardLoadUseCase>(MockBehavior.Strict),
            new Mock<IDashboardReloadSummaryService>(MockBehavior.Strict),
            new Mock<IDialogService>(MockBehavior.Strict));

    private sealed record DashboardLoadCoordinatorFixture(
        Mock<IDashboardLoadUseCase> LoadUseCase,
        Mock<IDashboardReloadSummaryService> ReloadSummaryService,
        Mock<IDialogService> DialogService)
    {
        public DashboardLoadCoordinator CreateCoordinator() =>
            new(LoadUseCase.Object, ReloadSummaryService.Object, DialogService.Object);
    }
}
