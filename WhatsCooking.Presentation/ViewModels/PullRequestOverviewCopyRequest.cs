namespace WhatsCooking.ViewModels;

/// <summary>
/// Pull requests selected for an AI team overview prompt.
/// </summary>
/// <param name="PullRequests">Pull request rows to analyze.</param>
/// <param name="IsMerged">Whether the pull requests are already merged.</param>
internal sealed record PullRequestOverviewCopyRequest(
    IReadOnlyList<PullRequestRow> PullRequests,
    bool IsMerged);
