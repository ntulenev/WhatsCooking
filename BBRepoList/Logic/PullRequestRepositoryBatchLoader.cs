using BBRepoList.Models;

namespace BBRepoList.Logic;

/// <summary>
/// Loads pull request data across repositories with bounded parallelism.
/// </summary>
public sealed class PullRequestRepositoryBatchLoader : IPullRequestRepositoryBatchLoader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TPullRequest>> LoadAsync<TPullRequest>(
        IReadOnlyList<Repository> repositories,
        int maxDegreeOfParallelism,
        Func<Repository, CancellationToken, Task<IReadOnlyList<TPullRequest>>> loadPullRequests,
        IProgress<PullRequestRepositoryLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repositories);
        ArgumentNullException.ThrowIfNull(loadPullRequests);

        var repositoriesToInspect = repositories
            .Where(static repository => repository.CanLoadPullRequests)
            .ToList();

        if (repositoriesToInspect.Count == 0)
        {
            return [];
        }

        var pullRequestsByRepository = new IReadOnlyList<TPullRequest>?[repositoriesToInspect.Count];
        var loadedRepositories = 0;

        progress?.Report(new PullRequestRepositoryLoadProgress(0, repositoriesToInspect.Count));

        await ForEachIndexAsync(
            repositoriesToInspect.Count,
            maxDegreeOfParallelism,
            async (index, token) =>
            {
                token.ThrowIfCancellationRequested();

                var repository = repositoriesToInspect[index];
                var pullRequests = await loadPullRequests(repository, token).ConfigureAwait(false);

                pullRequestsByRepository[index] = pullRequests;

                var currentLoaded = Interlocked.Increment(ref loadedRepositories);
                progress?.Report(new PullRequestRepositoryLoadProgress(currentLoaded, repositoriesToInspect.Count));
            },
            cancellationToken).ConfigureAwait(false);

        var pullRequestsByAllRepositories = new List<TPullRequest>();
        for (var index = 0; index < pullRequestsByRepository.Length; index++)
        {
            if (pullRequestsByRepository[index] is { } pullRequests)
            {
                pullRequestsByAllRepositories.AddRange(pullRequests);
            }
        }

        return pullRequestsByAllRepositories;
    }

    private static Task ForEachIndexAsync(
        int count,
        int maxDegreeOfParallelism,
        Func<int, CancellationToken, ValueTask> body,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDegreeOfParallelism, 1);
        ArgumentNullException.ThrowIfNull(body);

        return Parallel.ForEachAsync(
            Enumerable.Range(0, count),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            body);
    }
}
