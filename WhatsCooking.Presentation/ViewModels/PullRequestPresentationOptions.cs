using BBRepoList.Models;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Configuration values required to map pull requests for presentation.
/// </summary>
/// <param name="Workspace">Bitbucket workspace used to build links.</param>
/// <param name="MinimalDescriptionLength">Description length below which a warning is shown.</param>
/// <param name="TtfrThreshold">Open duration after which missing TTFR is highlighted.</param>
internal sealed record PullRequestPresentationOptions(
    BitbucketWorkspace Workspace,
    int MinimalDescriptionLength,
    TimeSpan TtfrThreshold);
