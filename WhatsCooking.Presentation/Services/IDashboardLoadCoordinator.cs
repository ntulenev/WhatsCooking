using BBRepoList.Models;

namespace WhatsCooking.Services;

/// <summary>
/// Coordinates dashboard load execution and reload-specific decisions.
/// </summary>
internal interface IDashboardLoadCoordinator
{
    /// <summary>
    /// Loads a dashboard snapshot and prepares reload metadata when needed.
    /// </summary>
    /// <param name="filterPattern">Repository filter pattern.</param>
    /// <param name="mergedPullRequestsDays">Number of days for merged pull requests.</param>
    /// <param name="isReload">Whether this load replaces already displayed pull requests.</param>
    /// <param name="previousOpenPullRequests">Previously loaded open pull requests.</param>
    /// <param name="previousMergedPullRequests">Previously loaded merged pull requests.</param>
    /// <param name="progress">Progress reporter for UI status updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard load coordinator result.</returns>
    Task<DashboardLoadCoordinatorResult> LoadAsync(
        FilterPattern filterPattern,
        int mergedPullRequestsDays,
        bool isReload,
        IReadOnlyCollection<PullRequestDetail> previousOpenPullRequests,
        IReadOnlyCollection<MergedPullRequest> previousMergedPullRequests,
        IProgress<PullRequestLoadProgress>? progress,
        CancellationToken cancellationToken);
}
