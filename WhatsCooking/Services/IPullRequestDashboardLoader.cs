using BBRepoList.Models;

namespace WhatsCooking.Services;

/// <summary>
/// Loads pull request dashboard data.
/// </summary>
internal interface IPullRequestDashboardLoader
{
    /// <summary>
    /// Loads repositories, open pull requests and recently merged pull requests.
    /// </summary>
    /// <param name="filterPattern">Repository filter pattern.</param>
    /// <param name="mergedPullRequestsDays">Number of days for merged pull requests.</param>
    /// <param name="progress">Progress reporter for UI status updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Loaded pull request dashboard data.</returns>
    Task<PullRequestLoadResult> LoadAsync(FilterPattern filterPattern, int mergedPullRequestsDays, IProgress<PullRequestLoadProgress>? progress, CancellationToken cancellationToken);
}
