using BBRepoList.Abstractions;
using BBRepoList.Models;

using FluentAssertions;

using Moq;

using WhatsCooking.Services;

namespace WhatsCooking.Tests;

public sealed class PullRequestDashboardLoaderTests
{
    [Fact(DisplayName = "Constructor throws when auth api is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenAuthApiIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketAuthApiClient authApi = null!;
        var repoService = new Mock<IRepoService>(MockBehavior.Strict).Object;

        // Act
        Action act = () => _ = new PullRequestDashboardLoader(authApi, repoService, TimeProvider.System);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when repository service is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenRepositoryServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        var authApi = new Mock<IBitbucketAuthApiClient>(MockBehavior.Strict).Object;
        IRepoService repoService = null!;

        // Act
        Action act = () => _ = new PullRequestDashboardLoader(authApi, repoService, TimeProvider.System);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when time provider is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTimeProviderIsNullThrowsArgumentNullException()
    {
        // Arrange
        var authApi = new Mock<IBitbucketAuthApiClient>(MockBehavior.Strict).Object;
        var repoService = new Mock<IRepoService>(MockBehavior.Strict).Object;
        TimeProvider timeProvider = null!;

        // Act
        Action act = () => _ = new PullRequestDashboardLoader(authApi, repoService, timeProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "LoadAsync throws when merged pull request days is not positive")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenMergedPullRequestDaysIsNotPositiveThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var loader = new PullRequestDashboardLoader(
            new Mock<IBitbucketAuthApiClient>(MockBehavior.Strict).Object,
            new Mock<IRepoService>(MockBehavior.Strict).Object,
            TimeProvider.System);

        // Act
        Func<Task> act = () => loader.LoadAsync(
            new FilterPattern(null),
            0,
            progress: null,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "LoadAsync loads and sorts complete pull request data")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenInputIsValidLoadsAndSortsCompletePullRequestData()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
        var currentUserId = new BitbucketId("{current-user}");
        var currentUser = new BitbucketUser(currentUserId, new UserName("Nikita"));
        var filterPattern = new FilterPattern("service", RepositorySearchMode.Contains);
        using var cts = new CancellationTokenSource();
        var repositoryB = new Repository("Billing", slug: new RepositorySlug("billing"));
        var repositoryA = new Repository("accounts", slug: new RepositorySlug("accounts"));
        IReadOnlyList<Repository> repositories = [repositoryB, repositoryA];
        IReadOnlyList<PullRequestDetail> openPullRequests = [];
        IReadOnlyList<MergedPullRequest> mergedPullRequests = [];
        var authCalls = 0;
        var repositoryCalls = 0;
        var openCalls = 0;
        var mergedCalls = 0;

        var authApi = new Mock<IBitbucketAuthApiClient>(MockBehavior.Strict);
        authApi.Setup(instance => instance.AuthSelfCheckAsync(cts.Token))
            .Callback(() => authCalls++)
            .ReturnsAsync(currentUser);
        var repoService = new Mock<IRepoService>(MockBehavior.Strict);
        repoService.Setup(instance => instance.GetRepositoriesAsync(
                filterPattern,
                It.Is<IProgress<RepoLoadProgress>?>(progress => progress != null),
                cts.Token))
            .Callback(() => repositoryCalls++)
            .ReturnsAsync(repositories);
        repoService.Setup(instance => instance.GetOpenPullRequestDetailsAsync(
                It.Is<IReadOnlyList<Repository>>(items =>
                    items.Count == 2
                    && ReferenceEquals(items[0], repositoryA)
                    && ReferenceEquals(items[1], repositoryB)),
                currentUserId,
                It.Is<IProgress<PullRequestRepositoryLoadProgress>?>(progress => progress != null),
                cts.Token))
            .Callback(() => openCalls++)
            .ReturnsAsync(openPullRequests);
        repoService.Setup(instance => instance.GetMergedPullRequestsAsync(
                It.Is<IReadOnlyList<Repository>>(items =>
                    items.Count == 2
                    && ReferenceEquals(items[0], repositoryA)
                    && ReferenceEquals(items[1], repositoryB)),
                now.AddDays(-14),
                currentUserId,
                It.Is<IProgress<PullRequestRepositoryLoadProgress>?>(progress => progress != null),
                cts.Token))
            .Callback(() => mergedCalls++)
            .ReturnsAsync(mergedPullRequests);
        var progress = new RecordingProgress<PullRequestLoadProgress>();
        var loader = new PullRequestDashboardLoader(authApi.Object, repoService.Object, new FixedTimeProvider(now));

        // Act
        var result = await loader.LoadAsync(filterPattern, 14, progress, cts.Token);

        // Assert
        result.Repositories.Should().Equal(repositoryA, repositoryB);
        result.OpenPullRequests.Should().BeSameAs(openPullRequests);
        result.MergedPullRequests.Should().BeSameAs(mergedPullRequests);
        progress.Values.Select(value => value.Stage).Should().ContainInOrder(
            PullRequestLoadStage.Authenticating,
            PullRequestLoadStage.Completed);
        authCalls.Should().Be(1);
        repositoryCalls.Should().Be(1);
        openCalls.Should().Be(1);
        mergedCalls.Should().Be(1);
        authApi.VerifyAll();
        repoService.VerifyAll();
    }
}
