using BBRepoList.Models;

namespace WhatsCooking.Services;

/// <summary>
/// Creates user-facing summaries for dashboard reload results.
/// </summary>
internal interface IDashboardReloadSummaryService
{
    /// <summary>
    /// Creates a reload summary by comparing previously loaded pull requests with the next dashboard snapshot.
    /// </summary>
    /// <param name="previousOpenPullRequests">Previously loaded open pull requests.</param>
    /// <param name="previousMergedPullRequests">Previously loaded merged pull requests.</param>
    /// <param name="nextSnapshot">Newly loaded dashboard snapshot.</param>
    /// <returns>User-facing reload summary.</returns>
    string CreateSummary(
        IReadOnlyCollection<PullRequestDetail> previousOpenPullRequests,
        IReadOnlyCollection<MergedPullRequest> previousMergedPullRequests,
        PullRequestDashboardSnapshot nextSnapshot);
}
