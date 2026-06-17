using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Stores pull request detail cache entries between application runs.
/// </summary>
public interface IPullRequestDetailsCache
{
    /// <summary>
    /// Gets the total size of the persisted pull request detail cache.
    /// </summary>
    /// <returns>Cache size in bytes, or 0 when cache is unavailable.</returns>
    long GetSizeInBytes();

    /// <summary>
    /// Reads cached pull request detail entries for a repository and current user.
    /// </summary>
    /// <param name="workspace">Bitbucket workspace identifier.</param>
    /// <param name="repositorySlug">Repository slug.</param>
    /// <param name="currentUserId">Current authenticated Bitbucket user identifier.</param>
    /// <param name="scope">Pull request population to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached entries, or an empty collection when cache is unavailable.</returns>
    Task<IReadOnlyList<PullRequestDetailsCacheEntry>> ReadEntriesAsync(
        BitbucketWorkspace workspace,
        RepositorySlug repositorySlug,
        BitbucketId currentUserId,
        PullRequestDetailsCacheScope scope,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves cached pull request detail entries for a repository and current user.
    /// </summary>
    /// <param name="workspace">Bitbucket workspace identifier.</param>
    /// <param name="repositorySlug">Repository slug.</param>
    /// <param name="currentUserId">Current authenticated Bitbucket user identifier.</param>
    /// <param name="scope">Pull request population to save.</param>
    /// <param name="entries">Entries to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveEntriesAsync(
        BitbucketWorkspace workspace,
        RepositorySlug repositorySlug,
        BitbucketId currentUserId,
        PullRequestDetailsCacheScope scope,
        IReadOnlyCollection<PullRequestDetailsCacheEntry> entries,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes cached pull request detail entries for a repository and current user.
    /// </summary>
    /// <param name="workspace">Bitbucket workspace identifier.</param>
    /// <param name="repositorySlug">Repository slug.</param>
    /// <param name="currentUserId">Current authenticated Bitbucket user identifier.</param>
    /// <param name="scope">Pull request population to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(
        BitbucketWorkspace workspace,
        RepositorySlug repositorySlug,
        BitbucketId currentUserId,
        PullRequestDetailsCacheScope scope,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes all persisted pull request detail cache entries.
    /// </summary>
    void Clear();
}
