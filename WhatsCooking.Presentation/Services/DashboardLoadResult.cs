namespace WhatsCooking.Services;

/// <summary>
/// Outcome of loading dashboard data.
/// </summary>
internal abstract record DashboardLoadResult
{
    private DashboardLoadResult()
    {
    }

    /// <summary>
    /// Successful dashboard load.
    /// </summary>
    /// <param name="Snapshot">Loaded dashboard snapshot.</param>
    internal sealed record Success(PullRequestDashboardSnapshot Snapshot) : DashboardLoadResult;

    /// <summary>
    /// Cancelled dashboard load.
    /// </summary>
    internal sealed record Cancelled : DashboardLoadResult;

    /// <summary>
    /// Failed dashboard load.
    /// </summary>
    /// <param name="UserMessage">Message suitable for display to the user.</param>
    internal sealed record Failure(string UserMessage) : DashboardLoadResult;
}
