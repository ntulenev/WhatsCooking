namespace WhatsCooking.Services;

/// <summary>
/// Stages of loading pull request dashboard data.
/// </summary>
internal enum PullRequestLoadStage
{
    /// <summary>
    /// Synthetic demo data is being prepared.
    /// </summary>
    LoadingDemoData,

    /// <summary>
    /// The current Bitbucket user is being authenticated.
    /// </summary>
    Authenticating,

    /// <summary>
    /// Repositories are being loaded.
    /// </summary>
    LoadingRepositories,

    /// <summary>
    /// Repositories are being scanned for open pull requests.
    /// </summary>
    LoadingOpenPullRequests,

    /// <summary>
    /// Repositories are being scanned for merged pull requests.
    /// </summary>
    LoadingMergedPullRequests,

    /// <summary>
    /// Dashboard loading has completed.
    /// </summary>
    Completed
}
