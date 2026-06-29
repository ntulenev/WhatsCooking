using BBRepoList.Models;

namespace BBRepoList.API;

/// <summary>
/// Loads Bitbucket open pull request data for a repository.
/// </summary>
public interface IBitbucketOpenPullRequestService
{
    /// <summary>
    /// Populates repository open pull requests count when available.
    /// </summary>
    /// <param name="repository">Repository to enrich.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PopulateOpenPullRequestCountAsync(Repository repository, CancellationToken cancellationToken);

    /// <summary>
    /// Loads open pull request details for repository report.
    /// </summary>
    /// <param name="repository">Repository to inspect.</param>
    /// <param name="currentUserId">Authenticated user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Open pull request details list for provided repository.</returns>
    Task<IReadOnlyList<PullRequestDetail>> GetOpenPullRequestDetailsAsync(
        Repository repository,
        BitbucketId currentUserId,
        CancellationToken cancellationToken);
}
