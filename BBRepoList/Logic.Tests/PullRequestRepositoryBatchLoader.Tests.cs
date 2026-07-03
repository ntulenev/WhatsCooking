using BBRepoList.Models;

using FluentAssertions;

namespace BBRepoList.Logic.Tests;

public sealed class PullRequestRepositoryBatchLoaderTests
{
    [Fact(DisplayName = "Load throws when repositories are null")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenRepositoriesAreNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new PullRequestRepositoryBatchLoader();
        IReadOnlyList<Repository> repositories = null!;

        // Act
        Func<Task> act = () => service.LoadAsync<int>(
            repositories,
            maxDegreeOfParallelism: 1,
            LoadEmptyAsync,
            progress: null,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "Load throws when loader delegate is null")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenLoaderDelegateIsNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new PullRequestRepositoryBatchLoader();
        Func<Repository, CancellationToken, Task<IReadOnlyList<int>>> loadPullRequests = null!;

        // Act
        Func<Task> act = () => service.LoadAsync(
            [CreateLoadableRepository("api")],
            maxDegreeOfParallelism: 1,
            loadPullRequests,
            progress: null,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "Load throws when maximum parallelism is less than one")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenMaximumParallelismIsLessThanOneThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var service = new PullRequestRepositoryBatchLoader();

        // Act
        Func<Task> act = () => service.LoadAsync<int>(
            [CreateLoadableRepository("api")],
            maxDegreeOfParallelism: 0,
            LoadEmptyAsync,
            progress: null,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Load skips repositories that cannot load pull requests")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenRepositoriesCannotLoadPullRequestsReturnsEmptyResult()
    {
        // Arrange
        var service = new PullRequestRepositoryBatchLoader();
        var progress = new RecordingProgress<PullRequestRepositoryLoadProgress>();

        // Act
        var result = await service.LoadAsync<int>(
            [new Repository("api"), new Repository("website")],
            maxDegreeOfParallelism: 1,
            (_, _) => throw new InvalidOperationException("Loader should not be called."),
            progress,
            CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        progress.Values.Should().BeEmpty();
    }

    [Fact(DisplayName = "Load returns pull requests in repository order and reports progress")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenRepositoriesAreLoadableReturnsResultsInRepositoryOrderAndReportsProgress()
    {
        // Arrange
        var service = new PullRequestRepositoryBatchLoader();
        var firstRepository = CreateLoadableRepository("api");
        var secondRepository = CreateLoadableRepository("website");
        var unavailableRepository = new Repository("legacy");
        var progress = new RecordingProgress<PullRequestRepositoryLoadProgress>();
        using var cancellation = new CancellationTokenSource();

        // Act
        var result = await service.LoadAsync(
            [firstRepository, unavailableRepository, secondRepository],
            maxDegreeOfParallelism: 1,
            (repository, token) =>
            {
                token.CanBeCanceled.Should().BeTrue();

                if (ReferenceEquals(repository, firstRepository))
                {
                    return Task.FromResult<IReadOnlyList<int>>([1, 2]);
                }

                if (ReferenceEquals(repository, secondRepository))
                {
                    return Task.FromResult<IReadOnlyList<int>>([3]);
                }

                throw new InvalidOperationException("Unavailable repository should not be loaded.");
            },
            progress,
            cancellation.Token);

        // Assert
        result.Should().Equal(1, 2, 3);
        progress.Values.Should().BeEquivalentTo(
            [
                new { LoadedRepositories = 0, TotalRepositories = 2 },
                new { LoadedRepositories = 1, TotalRepositories = 2 },
                new { LoadedRepositories = 2, TotalRepositories = 2 }
            ],
            options => options.WithStrictOrdering());
    }

    [Fact(DisplayName = "Load preserves repository order when parallel loads complete out of order")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenParallelLoadsCompleteOutOfOrderPreservesRepositoryOrder()
    {
        // Arrange
        var service = new PullRequestRepositoryBatchLoader();
        var firstRepository = CreateLoadableRepository("api");
        var secondRepository = CreateLoadableRepository("website");
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completeFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act
        var loadTask = service.LoadAsync<string>(
            [firstRepository, secondRepository],
            maxDegreeOfParallelism: 2,
            async (repository, _) =>
            {
                if (ReferenceEquals(repository, firstRepository))
                {
                    await secondStarted.Task.WaitAsync(
                        TimeSpan.FromSeconds(5),
                        TestContext.Current.CancellationToken);
                    await completeFirst.Task.WaitAsync(
                        TimeSpan.FromSeconds(5),
                        TestContext.Current.CancellationToken);
                    return ["first"];
                }

                secondStarted.SetResult();
                return ["second"];
            },
            progress: null,
            CancellationToken.None);

        await secondStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);
        completeFirst.SetResult();
        var result = await loadTask.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().Equal("first", "second");
    }

    [Fact(DisplayName = "Load respects maximum parallelism")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenMaximumParallelismIsProvidedLimitsConcurrentLoads()
    {
        // Arrange
        var service = new PullRequestRepositoryBatchLoader();
        Repository[] repositories =
        [
            CreateLoadableRepository("api"),
            CreateLoadableRepository("website"),
            CreateLoadableRepository("mobile")
        ];
        var releaseLoaders = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var twoLoadersStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedLoaders = 0;

        // Act
        var loadTask = service.LoadAsync<string>(
            repositories,
            maxDegreeOfParallelism: 2,
            async (repository, _) =>
            {
                var currentStarted = Interlocked.Increment(ref startedLoaders);
                if (currentStarted == 2)
                {
                    twoLoadersStarted.SetResult();
                }

                await releaseLoaders.Task.WaitAsync(
                    TimeSpan.FromSeconds(5),
                    TestContext.Current.CancellationToken);
                return [repository.Name];
            },
            progress: null,
            CancellationToken.None);

        try
        {
            await twoLoadersStarted.Task.WaitAsync(
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken);
            await Task.Delay(
                TimeSpan.FromMilliseconds(100),
                TestContext.Current.CancellationToken);
            startedLoaders.Should().Be(2);
        }
        finally
        {
            releaseLoaders.SetResult();
        }

        var result = await loadTask.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().Equal("api", "website", "mobile");
    }

    private static Repository CreateLoadableRepository(string name) =>
        new(name, slug: new RepositorySlug(name));

    private static Task<IReadOnlyList<int>> LoadEmptyAsync(
        Repository _,
        CancellationToken __) =>
        Task.FromResult<IReadOnlyList<int>>([]);

    private sealed class RecordingProgress<T> : IProgress<T>
    {
        private readonly Lock _syncRoot = new();

        public List<T> Values { get; } = [];

        public void Report(T value)
        {
            lock (_syncRoot)
            {
                Values.Add(value);
            }
        }
    }
}
