using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

namespace BBRepoList.API;

/// <summary>
/// Loads Bitbucket merged pull request data for a repository.
/// </summary>
public sealed class BitbucketMergedPullRequestService : IBitbucketMergedPullRequestService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketMergedPullRequestService"/> class.
    /// </summary>
    /// <param name="urlBuilder">Bitbucket pull request URL builder.</param>
    /// <param name="pageReader">Bitbucket pull request page reader.</param>
    /// <param name="snapshotMapper">Pull request snapshot mapper.</param>
    /// <param name="cacheService">Pull request details cache service.</param>
    /// <param name="activitySummaryProvider">Pull request activity summary provider.</param>
    /// <param name="domainFactory">Pull request domain model factory.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public BitbucketMergedPullRequestService(
        IBitbucketPullRequestUrlBuilder urlBuilder,
        IBitbucketPullRequestPageReader pageReader,
        IPullRequestSnapshotMapper snapshotMapper,
        IPullRequestDetailsCacheService cacheService,
        IPullRequestActivitySummaryProvider activitySummaryProvider,
        IPullRequestDomainFactory domainFactory,
        IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(urlBuilder);
        ArgumentNullException.ThrowIfNull(pageReader);
        ArgumentNullException.ThrowIfNull(snapshotMapper);
        ArgumentNullException.ThrowIfNull(cacheService);
        ArgumentNullException.ThrowIfNull(activitySummaryProvider);
        ArgumentNullException.ThrowIfNull(domainFactory);
        ArgumentNullException.ThrowIfNull(options);

        _urlBuilder = urlBuilder;
        _pageReader = pageReader;
        _snapshotMapper = snapshotMapper;
        _cacheService = cacheService;
        _activitySummaryProvider = activitySummaryProvider;
        _domainFactory = domainFactory;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MergedPullRequest>> GetMergedPullRequestsAsync(
        Repository repository,
        DateTimeOffset mergedSince,
        BitbucketId currentUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (!repository.CanLoadPullRequests)
        {
            return [];
        }

        var repositorySlug = repository.Slug!.Value;
        var workspace = new BitbucketWorkspace(_options.Workspace);
        var pullRequests = new List<MergedPullRequest>();

        try
        {
            var cacheEntriesByPullRequestId = await _cacheService
                .ReadEntriesByPullRequestIdAsync(
                    workspace,
                    repositorySlug,
                    currentUserId,
                    PullRequestDetailsCacheScope.Merged,
                    cancellationToken)
                .ConfigureAwait(false);
            var updatedCacheEntries = new List<PullRequestDetailsCacheEntry>();
            var url = _urlBuilder.CreateMergedPullRequestsUrl(repositorySlug);

            await _pageReader.ForEachAsync(
                url,
                async (pullRequestDto, token) =>
                {
                    if (pullRequestDto.Id is null
                        || pullRequestDto.Id <= 0
                        || pullRequestDto.CreatedOn is null
                        || pullRequestDto.UpdatedOn is null)
                    {
                        return true;
                    }

                    if (pullRequestDto.UpdatedOn.Value < mergedSince)
                    {
                        return false;
                    }

                    var pullRequest = _snapshotMapper.CreateSnapshot(pullRequestDto, currentUserId);
                    var activitySummary = await _activitySummaryProvider
                        .GetAsync(
                            repositorySlug,
                            pullRequest,
                            cacheEntriesByPullRequestId,
                            currentUserId,
                            token)
                        .ConfigureAwait(false);

                    pullRequests.Add(_domainFactory.CreateMerged(repository, pullRequest, pullRequestDto.UpdatedOn.Value, activitySummary.Summary));
                    updatedCacheEntries.Add(activitySummary.CacheEntry);
                    return true;
                },
                cancellationToken).ConfigureAwait(false);

            await _cacheService
                .SaveEntriesAsync(
                    workspace,
                    repositorySlug,
                    currentUserId,
                    PullRequestDetailsCacheScope.Merged,
                    updatedCacheEntries,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return [];
        }

        return pullRequests;
    }

    private readonly IBitbucketPullRequestUrlBuilder _urlBuilder;
    private readonly IBitbucketPullRequestPageReader _pageReader;
    private readonly IPullRequestSnapshotMapper _snapshotMapper;
    private readonly IPullRequestDetailsCacheService _cacheService;
    private readonly IPullRequestActivitySummaryProvider _activitySummaryProvider;
    private readonly IPullRequestDomainFactory _domainFactory;
    private readonly BitbucketOptions _options;
}
