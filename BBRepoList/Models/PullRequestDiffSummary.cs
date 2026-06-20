namespace BBRepoList.Models;

/// <summary>
/// Counts pull requests that were added between two dashboard loads.
/// </summary>
/// <param name="NewOpenPullRequestsCount">Number of open pull requests absent from the previous load.</param>
/// <param name="NewMergedPullRequestsCount">Number of merged pull requests absent from the previous load.</param>
public sealed record PullRequestDiffSummary(
    int NewOpenPullRequestsCount,
    int NewMergedPullRequestsCount)
{
    /// <summary>
    /// Gets a value indicating whether the compared pull request sets contain new pull requests.
    /// </summary>
    public bool HasNewPullRequests => NewOpenPullRequestsCount > 0 || NewMergedPullRequestsCount > 0;
}
