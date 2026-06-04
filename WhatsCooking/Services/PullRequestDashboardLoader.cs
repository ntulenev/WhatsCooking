using System.Diagnostics.CodeAnalysis;

using BBRepoList.Abstractions;
using BBRepoList.Models;

namespace WhatsCooking.Services;

/// <summary>
/// Loads pull request dashboard data from Bitbucket services.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Loader is created by dependency injection.")]
internal sealed class PullRequestDashboardLoader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestDashboardLoader"/> class.
    /// </summary>
    /// <param name="authApi">Bitbucket authentication API client.</param>
    /// <param name="repoService">Repository service used to load pull requests.</param>
    /// <param name="timeProvider">Time provider used to calculate the merged period.</param>
    public PullRequestDashboardLoader(IBitbucketAuthApiClient authApi, IRepoService repoService, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(authApi, nameof(authApi));
        ArgumentNullException.ThrowIfNull(repoService, nameof(repoService));
        ArgumentNullException.ThrowIfNull(timeProvider, nameof(timeProvider));
        _authApi = authApi;
        _repoService = repoService;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Loads repositories, open pull requests and recently merged pull requests.
    /// </summary>
    /// <param name="filterPattern">Repository filter pattern.</param>
    /// <param name="mergedPullRequestsDays">Number of days for merged pull requests.</param>
    /// <param name="progress">Progress reporter for UI status updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Loaded pull request dashboard data.</returns>
    public async Task<PullRequestLoadResult> LoadAsync(FilterPattern filterPattern, int mergedPullRequestsDays, IProgress<PullRequestLoadProgress>? progress, CancellationToken cancellationToken)
    {
        if (mergedPullRequestsDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mergedPullRequestsDays), "Merged pull request days must be greater than zero.");
        }
        progress?.Report(new PullRequestLoadProgress("Authenticating"));
        var currentUser = await _authApi.AuthSelfCheckAsync(cancellationToken).ConfigureAwait(false);
        var repositoryProgress = new Progress<RepoLoadProgress>(value => progress?.Report(new PullRequestLoadProgress("Loading repositories", value.Matched, value.Seen)));
        var repositories = await _repoService.GetRepositoriesAsync(filterPattern, repositoryProgress, cancellationToken).ConfigureAwait(false);
        List<Repository> sortedRepositories = [.. repositories.OrderBy(repository => repository.Name, StringComparer.OrdinalIgnoreCase)];
        var openPullRequestsProgress = new Progress<PullRequestRepositoryLoadProgress>(value => progress?.Report(new PullRequestLoadProgress("Scanning repositories for open pull requests", value.LoadedRepositories, value.TotalRepositories)));
        var openPullRequests = await _repoService.GetOpenPullRequestDetailsAsync(sortedRepositories, currentUser.Uuid, openPullRequestsProgress, cancellationToken).ConfigureAwait(false);
        var mergedPullRequestsProgress = new Progress<PullRequestRepositoryLoadProgress>(value => progress?.Report(new PullRequestLoadProgress("Scanning repositories for merged pull requests", value.LoadedRepositories, value.TotalRepositories)));
        var mergedSince = _timeProvider.GetLocalNow().AddDays(-mergedPullRequestsDays);
        var mergedPullRequests = await _repoService.GetMergedPullRequestsAsync(sortedRepositories, mergedSince, currentUser.Uuid, mergedPullRequestsProgress, cancellationToken).ConfigureAwait(false);
        progress?.Report(new PullRequestLoadProgress("Completed"));
        return new PullRequestLoadResult(sortedRepositories, openPullRequests, mergedPullRequests);
    }

    private readonly IBitbucketAuthApiClient _authApi;

    private readonly IRepoService _repoService;

    private readonly TimeProvider _timeProvider;
}
