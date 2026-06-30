using System.Runtime.CompilerServices;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace BBRepoList.Logic.Tests;

public sealed class RepositoryServiceTests
{
    [Fact(DisplayName = "Constructor throws when repository API is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenRepositoryApiIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketRepoApiClient api = null!;

        // Act
        Action act = () => _ = new RepositoryService(
            api,
            Mock.Of<IBitbucketPRApiClient>(),
            Mock.Of<IPullRequestRepositoryBatchLoader>(),
            CreateOptions());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when pull request API is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPullRequestApiIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketPRApiClient prApi = null!;

        // Act
        Action act = () => _ = new RepositoryService(
            Mock.Of<IBitbucketRepoApiClient>(),
            prApi,
            Mock.Of<IPullRequestRepositoryBatchLoader>(),
            CreateOptions());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new RepositoryService(
            Mock.Of<IBitbucketRepoApiClient>(),
            Mock.Of<IBitbucketPRApiClient>(),
            Mock.Of<IPullRequestRepositoryBatchLoader>(),
            options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Get repositories filters stream and reports cumulative progress")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenFilterIsProvidedReturnsMatchesAndReportsProgress()
    {
        // Arrange
        var filterPattern = new FilterPattern("api", RepositorySearchMode.Contains);
        using var cancellation = new CancellationTokenSource();
        Repository[] repositories =
        [
            new("API"),
            new("Website"),
            new("Public Api")
        ];
        var progress = new RecordingProgress<RepoLoadProgress>();
        var api = new Mock<IBitbucketRepoApiClient>(MockBehavior.Strict);
        api.Setup(instance => instance.GetRepositoriesAsync(
                filterPattern,
                It.Is<CancellationToken>(token => token == cancellation.Token)))
            .Returns(StreamRepositories(repositories, cancellation.Token));
        var service = CreateService(api: api.Object);

        // Act
        var result = await service.GetRepositoriesAsync(
            filterPattern,
            progress,
            cancellation.Token);

        // Assert
        result.Should().Equal(repositories[0], repositories[2]);
        progress.Values.Should().BeEquivalentTo(
            [
                new { Seen = 1, Matched = 1 },
                new { Seen = 2, Matched = 1 },
                new { Seen = 3, Matched = 2 }
            ],
            options => options.WithStrictOrdering());
        api.VerifyAll();
    }

    [Fact(DisplayName = "Get open pull request details returns empty result for empty repositories")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenRepositoriesAreEmptyReturnsEmptyResult()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var service = CreateService();

        // Act
        var result = await service.GetOpenPullRequestDetailsAsync(
            [],
            _currentUserId,
            progress: null,
            cancellation.Token);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "Get open pull request details throws when repositories are null")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenRepositoriesAreNullThrowsArgumentNullException()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var service = CreateService();
        IReadOnlyList<Repository> repositories = null!;

        // Act
        Func<Task> act = () => service.GetOpenPullRequestDetailsAsync(
            repositories,
            _currentUserId,
            progress: null,
            cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "Get open pull request details skips unavailable repositories and sorts results")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenRepositoriesAreLoadableReturnsSortedResults()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var firstRepository = new Repository("zeta", slug: new RepositorySlug("zeta"));
        var secondRepository = new Repository("Alpha", slug: new RepositorySlug("alpha"));
        var unavailableRepository = new Repository("Unavailable");
        var latest = CreateOpenPullRequest(secondRepository, 3, new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));
        var sameDateZeta = CreateOpenPullRequest(firstRepository, 2, new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));
        var sameDateAlphaHighId = CreateOpenPullRequest(secondRepository, 4, sameDateZeta.OpenedOn);
        var sameDateAlphaLowId = CreateOpenPullRequest(secondRepository, 1, sameDateZeta.OpenedOn);
        var progress = new RecordingProgress<PullRequestRepositoryLoadProgress>();
        var prApi = new Mock<IBitbucketPRApiClient>(MockBehavior.Strict);
        prApi.Setup(instance => instance.GetOpenPullRequestDetailsAsync(
                firstRepository,
                _currentUserId,
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync([sameDateZeta]);
        prApi.Setup(instance => instance.GetOpenPullRequestDetailsAsync(
                secondRepository,
                _currentUserId,
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync([sameDateAlphaHighId, latest, sameDateAlphaLowId]);
        var service = CreateService(prApi: prApi.Object);

        // Act
        var result = await service.GetOpenPullRequestDetailsAsync(
            [firstRepository, unavailableRepository, secondRepository],
            _currentUserId,
            progress,
            cancellation.Token);

        // Assert
        result.Should().Equal(latest, sameDateAlphaLowId, sameDateAlphaHighId, sameDateZeta);
        progress.Values.Should().BeEquivalentTo(
            [
                new { LoadedRepositories = 0, TotalRepositories = 2 },
                new { LoadedRepositories = 1, TotalRepositories = 2 },
                new { LoadedRepositories = 2, TotalRepositories = 2 }
            ],
            options => options.WithStrictOrdering());
        prApi.VerifyAll();
    }

    [Fact(DisplayName = "Get merged pull requests passes boundary and sorts results")]
    [Trait("Category", "Unit")]
    public async Task GetMergedPullRequestsAsyncWhenRepositoriesAreLoadableReturnsSortedResults()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var mergedSince = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var firstRepository = new Repository("zeta", slug: new RepositorySlug("zeta"));
        var secondRepository = new Repository("Alpha", slug: new RepositorySlug("alpha"));
        var latest = CreateMergedPullRequest(secondRepository, 3, new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));
        var sameDateZeta = CreateMergedPullRequest(firstRepository, 2, new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));
        var sameDateAlphaHighId = CreateMergedPullRequest(secondRepository, 4, sameDateZeta.MergedOn);
        var sameDateAlphaLowId = CreateMergedPullRequest(secondRepository, 1, sameDateZeta.MergedOn);
        var prApi = new Mock<IBitbucketPRApiClient>(MockBehavior.Strict);
        prApi.Setup(instance => instance.GetMergedPullRequestsAsync(
                firstRepository,
                mergedSince,
                _currentUserId,
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync([sameDateZeta]);
        prApi.Setup(instance => instance.GetMergedPullRequestsAsync(
                secondRepository,
                mergedSince,
                _currentUserId,
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync([sameDateAlphaHighId, latest, sameDateAlphaLowId]);
        var service = CreateService(prApi: prApi.Object);

        // Act
        var result = await service.GetMergedPullRequestsAsync(
            [firstRepository, secondRepository],
            mergedSince,
            _currentUserId,
            progress: null,
            cancellation.Token);

        // Assert
        result.Should().Equal(latest, sameDateAlphaLowId, sameDateAlphaHighId, sameDateZeta);
        prApi.VerifyAll();
    }

    [Fact(DisplayName = "Get merged pull requests skips repositories without slug")]
    [Trait("Category", "Unit")]
    public async Task GetMergedPullRequestsAsyncWhenRepositoriesCannotLoadPullRequestsReturnsEmptyResult()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var service = CreateService();

        // Act
        var result = await service.GetMergedPullRequestsAsync(
            [new Repository("Unavailable")],
            new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            _currentUserId,
            progress: null,
            cancellation.Token);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "Get merged pull requests returns empty result for empty repositories")]
    [Trait("Category", "Unit")]
    public async Task GetMergedPullRequestsAsyncWhenRepositoriesAreEmptyReturnsEmptyResult()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var service = CreateService();

        // Act
        var result = await service.GetMergedPullRequestsAsync(
            [],
            new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            _currentUserId,
            progress: null,
            cancellation.Token);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "Get open pull request details observes cancellation")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenCancelledThrowsOperationCanceledException()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var service = CreateService();
        var repository = new Repository("API", slug: new RepositorySlug("api"));

        // Act
        Func<Task> act = () => service.GetOpenPullRequestDetailsAsync(
            [repository],
            _currentUserId,
            progress: null,
            cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static RepositoryService CreateService(
        IBitbucketRepoApiClient? api = null,
        IBitbucketPRApiClient? prApi = null) =>
        new(
            api ?? new Mock<IBitbucketRepoApiClient>(MockBehavior.Strict).Object,
            prApi ?? new Mock<IBitbucketPRApiClient>(MockBehavior.Strict).Object,
            new PullRequestRepositoryBatchLoader(),
            CreateOptions());

    private static IOptions<BitbucketOptions> CreateOptions() =>
        Options.Create(new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/"),
            PullRequestDetails = new PullRequestDetailsOptions
            {
                LoadThreshold = 1
            },
            MergedPullRequests = new MergedPullRequestsOptions
            {
                LoadThreshold = 1
            }
        });

    private static PullRequestDetail CreateOpenPullRequest(
        Repository repository,
        int id,
        DateTimeOffset openedOn) =>
        new(
            repository,
            new PullRequestId(id),
            $"PR {id}",
            openedOn,
            authorId: null,
            authorDisplayName: null,
            firstNonAuthorActivityOn: null,
            lastActivityOn: null,
            hasCurrentUserDiscussion: false);

    private static MergedPullRequest CreateMergedPullRequest(
        Repository repository,
        int id,
        DateTimeOffset mergedOn) =>
        new(
            repository,
            new PullRequestId(id),
            $"PR {id}",
            mergedOn.AddDays(-1),
            authorId: null,
            authorDisplayName: null,
            firstNonAuthorActivityOn: null,
            lastActivityOn: null,
            hasCurrentUserDiscussion: false,
            mergedOn);

    private static async IAsyncEnumerable<Repository> StreamRepositories(
        IEnumerable<Repository> repositories,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var repository in repositories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return repository;
            await Task.Yield();
        }
    }

    private static readonly BitbucketId _currentUserId = new("current-user");

    private sealed class RecordingProgress<T> : IProgress<T>
    {
        public List<T> Values { get; } = [];

        public void Report(T value) => Values.Add(value);
    }
}
