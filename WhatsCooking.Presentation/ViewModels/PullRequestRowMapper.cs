using System.Diagnostics.CodeAnalysis;

using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Maps pull request models to dashboard rows.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Factory is created by dependency injection.")]
internal sealed class PullRequestRowMapper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestRowMapper"/> class.
    /// </summary>
    /// <param name="options">Bitbucket configuration options.</param>
    public PullRequestRowMapper(IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var value = options.Value;
        var workspace = string.IsNullOrWhiteSpace(value.Workspace) && value.DemoMode
            ? "demo-workspace"
            : value.Workspace;
        _options = new PullRequestPresentationOptions(
            new BitbucketWorkspace(workspace),
            value.PullRequestDetails.MinimalDescriptionTextLength,
            TimeSpan.FromHours(value.PullRequestDetails.TtfrThresholdHours));
    }

    /// <summary>
    /// Creates a row for an open pull request.
    /// </summary>
    /// <param name="number">Row number.</param>
    /// <param name="detail">Open pull request detail.</param>
    /// <param name="asOf">Timestamp used to calculate relative durations.</param>
    /// <returns>Pull request row.</returns>
    public PullRequestRow MapOpen(int number, PullRequestDetail detail, DateTimeOffset asOf) =>
        new(number, detail, asOf, _options);

    /// <summary>
    /// Creates a row for a merged pull request.
    /// </summary>
    /// <param name="number">Row number.</param>
    /// <param name="pullRequest">Merged pull request.</param>
    /// <param name="asOf">Timestamp used to calculate relative durations.</param>
    /// <returns>Pull request row.</returns>
    public PullRequestRow MapMerged(int number, MergedPullRequest pullRequest, DateTimeOffset asOf) =>
        new(number, pullRequest, asOf, _options);

    private readonly PullRequestPresentationOptions _options;
}
