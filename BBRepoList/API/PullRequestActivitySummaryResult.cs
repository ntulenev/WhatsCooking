using BBRepoList.Models;

namespace BBRepoList.API;

/// <summary>
/// Pull request activity summary with the cache entry that should be persisted.
/// </summary>
/// <param name="Summary">Pull request activity summary.</param>
/// <param name="CacheEntry">Cache entry matching the summary.</param>
public sealed record PullRequestActivitySummaryResult(
    PullRequestActivitySummary Summary,
    PullRequestDetailsCacheEntry CacheEntry);
