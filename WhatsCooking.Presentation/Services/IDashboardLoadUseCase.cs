using BBRepoList.Models;

namespace WhatsCooking.Services;

/// <summary>
/// Loads a complete dashboard snapshot for presentation.
/// </summary>
internal interface IDashboardLoadUseCase
{
    /// <summary>
    /// Loads dashboard data for the specified repository filter.
    /// </summary>
    /// <param name="filterPattern">Repository filter.</param>
    /// <param name="mergedPullRequestsDays">Recently merged period in days.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard load outcome.</returns>
    Task<DashboardLoadResult> LoadAsync(
        FilterPattern filterPattern,
        int mergedPullRequestsDays,
        IProgress<PullRequestLoadProgress>? progress,
        CancellationToken cancellationToken);
}
