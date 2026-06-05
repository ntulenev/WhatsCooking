namespace WhatsCooking.Services;

/// <summary>
/// Creates synthetic pull request dashboard data for demo mode.
/// </summary>
internal interface IDemoPullRequestDashboardProvider
{
    /// <summary>
    /// Creates demo repositories, open pull requests and recently merged pull requests.
    /// </summary>
    /// <returns>Demo pull request dashboard data.</returns>
    PullRequestLoadResult Create();
}
