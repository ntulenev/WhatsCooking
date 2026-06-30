using BBRepoList.Models;

namespace BBRepoList.Logic;

/// <summary>
/// Loads pull request data across repositories with bounded parallelism.
/// </summary>
public interface IPullRequestRepositoryBatchLoader
{
    /// <summary>
    /// Loads pull request data from repositories that support pull request loading.
    /// </summary>
    /// <typeparam name="TPullRequest">Pull request result type.</typeparam>
    /// <param name="repositories">Repositories to inspect.</param>
    /// <param name="maxDegreeOfParallelism">Maximum number of repositories loaded in parallel.</param>
    /// <param name="loadPullRequests">Repository pull request loader.</param>
    /// <param name="progress">Optional repository progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Loaded pull request data in repository order.</returns>
    Task<IReadOnlyList<TPullRequest>> LoadAsync<TPullRequest>(
        IReadOnlyList<Repository> repositories,
        int maxDegreeOfParallelism,
        Func<Repository, CancellationToken, Task<IReadOnlyList<TPullRequest>>> loadPullRequests,
        IProgress<PullRequestRepositoryLoadProgress>? progress,
        CancellationToken cancellationToken);
}
