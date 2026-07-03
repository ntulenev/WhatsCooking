using System.Runtime.CompilerServices;

using BBRepoList.Abstractions;
using BBRepoList.Models;

using FluentAssertions;

using Moq;

namespace BBRepoList.Logic.Tests;

public sealed class RepositoryQueryServiceTests
{
    [Fact(DisplayName = "Constructor throws when API is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenApiIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketRepoApiClient api = null!;

        // Act
        Action act = () => _ = new RepositoryQueryService(api);

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
        var service = new RepositoryQueryService(api.Object);

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

    [Fact(DisplayName = "Get repositories returns all streamed repositories when filter is empty")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenFilterIsEmptyReturnsAllRepositories()
    {
        // Arrange
        var filterPattern = new FilterPattern(null);
        using var cancellation = new CancellationTokenSource();
        Repository[] repositories =
        [
            new("API"),
            new("Website")
        ];
        var api = new Mock<IBitbucketRepoApiClient>(MockBehavior.Strict);
        api.Setup(instance => instance.GetRepositoriesAsync(
                filterPattern,
                It.Is<CancellationToken>(token => token == cancellation.Token)))
            .Returns(StreamRepositories(repositories, cancellation.Token));
        var service = new RepositoryQueryService(api.Object);

        // Act
        var result = await service.GetRepositoriesAsync(
            filterPattern,
            progress: null,
            cancellation.Token);

        // Assert
        result.Should().Equal(repositories);
        api.VerifyAll();
    }

    [Fact(DisplayName = "Get repositories observes cancellation while streaming")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenCancellationIsRequestedThrowsOperationCanceledException()
    {
        // Arrange
        var filterPattern = new FilterPattern("api", RepositorySearchMode.Contains);
        using var cancellation = new CancellationTokenSource();
        var api = new Mock<IBitbucketRepoApiClient>(MockBehavior.Strict);
        api.Setup(instance => instance.GetRepositoriesAsync(
                filterPattern,
                It.Is<CancellationToken>(token => token == cancellation.Token)))
            .Returns(StreamAndCancel(cancellation));
        var service = new RepositoryQueryService(api.Object);

        // Act
        Func<Task> act = () => service.GetRepositoriesAsync(
            filterPattern,
            progress: null,
            cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        api.VerifyAll();
    }

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

    private static async IAsyncEnumerable<Repository> StreamAndCancel(
        CancellationTokenSource cancellation)
    {
        yield return new Repository("API");
        await cancellation.CancelAsync();
        cancellation.Token.ThrowIfCancellationRequested();
    }

    private sealed class RecordingProgress<T> : IProgress<T>
    {
        public List<T> Values { get; } = [];

        public void Report(T value) => Values.Add(value);
    }
}
