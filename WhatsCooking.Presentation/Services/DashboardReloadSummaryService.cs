using BBRepoList.Abstractions;
using BBRepoList.Models;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Services;

/// <summary>
/// Creates user-facing summaries for dashboard reload results.
/// </summary>
internal sealed class DashboardReloadSummaryService : IDashboardReloadSummaryService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardReloadSummaryService"/> class.
    /// </summary>
    /// <param name="pullRequestDiffService">Pull request diff service.</param>
    public DashboardReloadSummaryService(IPullRequestDiffService pullRequestDiffService)
    {
        ArgumentNullException.ThrowIfNull(pullRequestDiffService);

        _pullRequestDiffService = pullRequestDiffService;
    }

    /// <inheritdoc />
    public string CreateSummary(
        IReadOnlyCollection<PullRequestDetail> previousOpenPullRequests,
        IReadOnlyCollection<MergedPullRequest> previousMergedPullRequests,
        PullRequestDashboardSnapshot nextSnapshot)
    {
        ArgumentNullException.ThrowIfNull(previousOpenPullRequests);
        ArgumentNullException.ThrowIfNull(previousMergedPullRequests);
        ArgumentNullException.ThrowIfNull(nextSnapshot);

        return PullRequestReloadSummaryFormatter.Format(_pullRequestDiffService.Compare(
            previousOpenPullRequests,
            previousMergedPullRequests,
            nextSnapshot.OpenPullRequests,
            nextSnapshot.MergedPullRequests));
    }

    private readonly IPullRequestDiffService _pullRequestDiffService;
}
