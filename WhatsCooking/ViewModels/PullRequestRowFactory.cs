using System.Diagnostics.CodeAnalysis;

using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Creates pull request rows for dashboard grids.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Factory is created by dependency injection.")]
internal sealed class PullRequestRowFactory : IPullRequestRowFactory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestRowFactory"/> class.
    /// </summary>
    /// <param name="options">Bitbucket configuration options.</param>
    public PullRequestRowFactory(IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    /// <summary>
    /// Creates a row for an open pull request.
    /// </summary>
    /// <param name="number">Row number.</param>
    /// <param name="detail">Open pull request detail.</param>
    /// <param name="asOf">Timestamp used to calculate relative durations.</param>
    /// <returns>Pull request row.</returns>
    public PullRequestRow CreateOpenRow(int number, PullRequestDetail detail, DateTimeOffset asOf) =>
        new(number, detail, asOf, _options);

    /// <summary>
    /// Creates a row for a merged pull request.
    /// </summary>
    /// <param name="number">Row number.</param>
    /// <param name="pullRequest">Merged pull request.</param>
    /// <returns>Pull request row.</returns>
    public PullRequestRow CreateMergedRow(int number, MergedPullRequest pullRequest) =>
        new(number, pullRequest, _options);

    private readonly BitbucketOptions _options;
}
