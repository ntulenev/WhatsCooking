namespace WhatsCooking.Services;

/// <summary>
/// Result of dashboard load coordination.
/// </summary>
internal abstract record DashboardLoadCoordinatorResult
{
    private DashboardLoadCoordinatorResult()
    {
    }

    /// <summary>
    /// Dashboard load completed successfully.
    /// </summary>
    /// <param name="Snapshot">Loaded dashboard snapshot.</param>
    /// <param name="ReloadSummary">Optional reload summary to show after applying the snapshot.</param>
    internal sealed record Success(PullRequestDashboardSnapshot Snapshot, string? ReloadSummary) : DashboardLoadCoordinatorResult;

    /// <summary>
    /// Dashboard load was cancelled.
    /// </summary>
    internal sealed record Cancelled : DashboardLoadCoordinatorResult;

    /// <summary>
    /// Dashboard load failed with a user-facing message.
    /// </summary>
    /// <param name="UserMessage">User-facing failure message.</param>
    internal sealed record Failure(string UserMessage) : DashboardLoadCoordinatorResult;

    /// <summary>
    /// Dashboard load was skipped before calling the load use case.
    /// </summary>
    internal sealed record Skipped : DashboardLoadCoordinatorResult;
}
