using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

namespace BBRepoList.Logic;

/// <summary>
/// Loads repositories from Bitbucket with optional name filtering.
/// </summary>
public sealed class RepositoryService : IRepoService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryService"/> class.
    /// </summary>
    /// <param name="api">Bitbucket API client.</param>
    /// <param name="prApi">Bitbucket pull request API client.</param>
    /// <param name="batchLoader">Pull request batch loader.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public RepositoryService(
        IBitbucketRepoApiClient api,
        IBitbucketPRApiClient prApi,
        IPullRequestRepositoryBatchLoader batchLoader,
        IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentNullException.ThrowIfNull(prApi);
        ArgumentNullException.ThrowIfNull(batchLoader);
        ArgumentNullException.ThrowIfNull(options);

        _api = api;
        _prApi = prApi;
        _batchLoader = batchLoader;
        _pullRequestDetailsLoadThreshold = options.Value.PullRequestDetails.LoadThreshold;
        _mergedPullRequestsLoadThreshold = options.Value.MergedPullRequests.LoadThreshold;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Repository>> GetRepositoriesAsync(
        FilterPattern filterPattern,
        IProgress<RepoLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var matchedRepositories = new List<Repository>();

        var seen = 0;
        var matched = 0;

        await foreach (var repository in _api.GetRepositoriesAsync(filterPattern, cancellationToken).ConfigureAwait(false))
        {
            seen++;

            if (filterPattern.Filter(repository))
            {
                matched++;
                matchedRepositories.Add(repository);
            }

            progress?.Report(new RepoLoadProgress(seen, matched));
        }

        return matchedRepositories;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PullRequestDetail>> GetOpenPullRequestDetailsAsync(
        IReadOnlyList<Repository> repositories,
        BitbucketId currentUserId,
        IProgress<PullRequestRepositoryLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        if (repositories.Count == 0)
        {
            return [];
        }

        var pullRequestDetails = await _batchLoader.LoadAsync(
            repositories,
            _pullRequestDetailsLoadThreshold,
            (repository, token) =>
                _prApi.GetOpenPullRequestDetailsAsync(repository, currentUserId, token),
            progress,
            cancellationToken).ConfigureAwait(false);

        return
        [
            .. pullRequestDetails
                .OrderByDescending(static detail => detail.OpenedOn)
                .ThenBy(static detail => detail.RepositoryName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static detail => detail.PullRequestId.Value)
        ];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MergedPullRequest>> GetMergedPullRequestsAsync(
        IReadOnlyList<Repository> repositories,
        DateTimeOffset mergedSince,
        BitbucketId currentUserId,
        IProgress<PullRequestRepositoryLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        if (repositories.Count == 0)
        {
            return [];
        }

        var mergedPullRequests = await _batchLoader.LoadAsync(
            repositories,
            _mergedPullRequestsLoadThreshold,
            (repository, token) =>
                _prApi.GetMergedPullRequestsAsync(repository, mergedSince, currentUserId, token),
            progress,
            cancellationToken).ConfigureAwait(false);

        return
        [
            .. mergedPullRequests
                .OrderByDescending(static pullRequest => pullRequest.MergedOn)
                .ThenBy(static pullRequest => pullRequest.RepositoryName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static pullRequest => pullRequest.PullRequestId.Value)
        ];
    }

    private readonly IBitbucketRepoApiClient _api;
    private readonly IBitbucketPRApiClient _prApi;
    private readonly IPullRequestRepositoryBatchLoader _batchLoader;
    private readonly int _pullRequestDetailsLoadThreshold;
    private readonly int _mergedPullRequestsLoadThreshold;
}

