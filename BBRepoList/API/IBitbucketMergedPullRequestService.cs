using BBRepoList.Models;

namespace BBRepoList.API;

/// <summary>
/// Loads Bitbucket merged pull request data for a repository.
/// </summary>
public interface IBitbucketMergedPullRequestService
{
    /// <summary>
    /// Loads recently merged pull requests for repository report.
    /// </summary>
    /// <param name="repository">Repository to inspect.</param>
    /// <param name="mergedSince">Inclusive lower bound for pull request merge timestamp.</param>
    /// <param name="currentUserId">Authenticated user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recently merged pull requests for provided repository.</returns>
    Task<IReadOnlyList<MergedPullRequest>> GetMergedPullRequestsAsync(
        Repository repository,
        DateTimeOffset mergedSince,
        BitbucketId currentUserId,
        CancellationToken cancellationToken);
}
