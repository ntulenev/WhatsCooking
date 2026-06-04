using BBRepoList.Models;

namespace WhatsCooking.Services;

/// <summary>
/// Pull request dashboard data loaded from Bitbucket.
/// </summary>
/// <param name="Repositories">Repositories matched by the repository filter.</param>
/// <param name="OpenPullRequests">Open pull request details.</param>
/// <param name="MergedPullRequests">Recently merged pull requests.</param>
internal sealed record PullRequestLoadResult(
    IReadOnlyList<Repository> Repositories,
    IReadOnlyList<PullRequestDetail> OpenPullRequests,
    IReadOnlyList<MergedPullRequest> MergedPullRequests);
