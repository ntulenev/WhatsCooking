using BBRepoList.Models;

namespace BBRepoList.Logic;

/// <summary>
/// Sorts pull request results for repository reports.
/// </summary>
public interface IPullRequestResultSorter
{
    /// <summary>
    /// Sorts open pull request details for display.
    /// </summary>
    /// <param name="pullRequests">Open pull request details.</param>
    /// <returns>Sorted open pull request details.</returns>
    IReadOnlyList<PullRequestDetail> SortOpen(IReadOnlyList<PullRequestDetail> pullRequests);

    /// <summary>
    /// Sorts merged pull requests for display.
    /// </summary>
    /// <param name="pullRequests">Merged pull requests.</param>
    /// <returns>Sorted merged pull requests.</returns>
    IReadOnlyList<MergedPullRequest> SortMerged(IReadOnlyList<MergedPullRequest> pullRequests);
}
