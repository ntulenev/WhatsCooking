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
    /// <param name="repositoryQuery">Repository query service.</param>
    /// <param name="prApi">Bitbucket pull request API client.</param>
    /// <param name="batchLoader">Pull request batch loader.</param>
    /// <param name="resultSorter">Pull request result sorter.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public RepositoryService(
        IRepositoryQueryService repositoryQuery,
        IBitbucketPRApiClient prApi,
        IPullRequestRepositoryBatchLoader batchLoader,
        IPullRequestResultSorter resultSorter,
        IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(repositoryQuery);
        ArgumentNullException.ThrowIfNull(prApi);
        ArgumentNullException.ThrowIfNull(batchLoader);
        ArgumentNullException.ThrowIfNull(resultSorter);
        ArgumentNullException.ThrowIfNull(options);

        _repositoryQuery = repositoryQuery;
        _prApi = prApi;
        _batchLoader = batchLoader;
        _resultSorter = resultSorter;
        _pullRequestDetailsLoadThreshold = options.Value.PullRequestDetails.LoadThreshold;
        _mergedPullRequestsLoadThreshold = options.Value.MergedPullRequests.LoadThreshold;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Repository>> GetRepositoriesAsync(
        FilterPattern filterPattern,
        IProgress<RepoLoadProgress>? progress,
        CancellationToken cancellationToken) =>
        _repositoryQuery.GetRepositoriesAsync(filterPattern, progress, cancellationToken);

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

        return _resultSorter.SortOpen(pullRequestDetails);
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

        return _resultSorter.SortMerged(mergedPullRequests);
    }

    private readonly IRepositoryQueryService _repositoryQuery;
    private readonly IBitbucketPRApiClient _prApi;
    private readonly IPullRequestRepositoryBatchLoader _batchLoader;
    private readonly IPullRequestResultSorter _resultSorter;
    private readonly int _pullRequestDetailsLoadThreshold;
    private readonly int _mergedPullRequestsLoadThreshold;
}

