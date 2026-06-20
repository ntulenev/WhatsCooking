using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Compares pull request collections between dashboard loads.
/// </summary>
public interface IPullRequestDiffService
{
    /// <summary>
    /// Counts pull requests that exist in the current load but not in the previous load.
    /// </summary>
    /// <param name="previousOpenPullRequests">Open pull requests from the previous load.</param>
    /// <param name="previousMergedPullRequests">Merged pull requests from the previous load.</param>
    /// <param name="currentOpenPullRequests">Open pull requests from the current load.</param>
    /// <param name="currentMergedPullRequests">Merged pull requests from the current load.</param>
    /// <returns>Counts of newly added pull requests.</returns>
    PullRequestDiffSummary Compare(
        IReadOnlyCollection<PullRequestDetail> previousOpenPullRequests,
        IReadOnlyCollection<MergedPullRequest> previousMergedPullRequests,
        IReadOnlyCollection<PullRequestDetail> currentOpenPullRequests,
        IReadOnlyCollection<MergedPullRequest> currentMergedPullRequests);
}
