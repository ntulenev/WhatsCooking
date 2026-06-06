using BBRepoList.Models;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Creates pull request rows for dashboard grids.
/// </summary>
internal interface IPullRequestRowFactory
{
    /// <summary>
    /// Creates a row for an open pull request.
    /// </summary>
    /// <param name="number">Row number.</param>
    /// <param name="detail">Open pull request detail.</param>
    /// <param name="asOf">Timestamp used to calculate relative durations.</param>
    /// <returns>Pull request row.</returns>
    PullRequestRow CreateOpenRow(int number, PullRequestDetail detail, DateTimeOffset asOf);

    /// <summary>
    /// Creates a row for a merged pull request.
    /// </summary>
    /// <param name="number">Row number.</param>
    /// <param name="pullRequest">Merged pull request.</param>
    /// <param name="asOf">Timestamp used to calculate relative durations.</param>
    /// <returns>Pull request row.</returns>
    PullRequestRow CreateMergedRow(int number, MergedPullRequest pullRequest, DateTimeOffset asOf);
}
