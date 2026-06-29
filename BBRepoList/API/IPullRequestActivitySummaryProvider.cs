using BBRepoList.Models;

namespace BBRepoList.API;

/// <summary>
/// Resolves pull request activity summaries from cache or Bitbucket activity data.
/// </summary>
public interface IPullRequestActivitySummaryProvider
{
    /// <summary>
    /// Gets a pull request activity summary and cache entry.
    /// </summary>
    /// <param name="repositorySlug">Repository slug.</param>
    /// <param name="pullRequest">Pull request snapshot.</param>
    /// <param name="cachedEntries">Cached entries keyed by pull request identifier.</param>
    /// <param name="currentUserId">Authenticated user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Activity summary result.</returns>
    Task<PullRequestActivitySummaryResult> GetAsync(
        RepositorySlug repositorySlug,
        PullRequestSnapshot pullRequest,
        IReadOnlyDictionary<PullRequestId, PullRequestDetailsCacheEntry> cachedEntries,
        BitbucketId currentUserId,
        CancellationToken cancellationToken);
}
