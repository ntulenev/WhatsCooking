using BBRepoList.Models;

namespace WhatsCooking.Services;

/// <summary>
/// Complete pull request dashboard data ready to apply to the UI.
/// </summary>
/// <param name="AsOf">Timestamp used to calculate relative pull request durations.</param>
/// <param name="Repositories">Repositories matched by the repository filter.</param>
/// <param name="OpenPullRequests">Open pull request details.</param>
/// <param name="MergedPullRequests">Recently merged pull requests.</param>
/// <param name="Telemetry">Bitbucket API telemetry snapshot.</param>
internal sealed record PullRequestDashboardSnapshot(
    DateTimeOffset AsOf,
    IReadOnlyList<Repository> Repositories,
    IReadOnlyList<PullRequestDetail> OpenPullRequests,
    IReadOnlyList<MergedPullRequest> MergedPullRequests,
    BitbucketTelemetrySnapshot Telemetry);
