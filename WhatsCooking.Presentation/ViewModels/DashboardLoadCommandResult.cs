namespace WhatsCooking.ViewModels;

/// <summary>
/// Bindable summary returned after a dashboard load command finishes.
/// </summary>
internal sealed record DashboardLoadCommandResult(
    string Status,
    int? RepositoriesCount = null,
    int? OpenPullRequestsCount = null,
    int? MergedPullRequestsCount = null,
    string? LoadedAt = null)
{
    /// <summary>
    /// Result returned when a dashboard load is cancelled.
    /// </summary>
    public static DashboardLoadCommandResult Cancelled { get; } = new("Cancelled");

    /// <summary>
    /// Result returned when a dashboard load is skipped.
    /// </summary>
    public static DashboardLoadCommandResult Skipped { get; } = new(string.Empty);

    /// <summary>
    /// Creates a failed load result.
    /// </summary>
    public static DashboardLoadCommandResult Failure(string message) => new(message);
}
