using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Transport;

using Microsoft.Extensions.Options;

namespace BBRepoList.API;

/// <summary>
/// Loads Bitbucket open pull request data for a repository.
/// </summary>
public sealed class BitbucketOpenPullRequestService : IBitbucketOpenPullRequestService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketOpenPullRequestService"/> class.
    /// </summary>
    /// <param name="transport">Bitbucket transport instance.</param>
    /// <param name="urlBuilder">Bitbucket pull request URL builder.</param>
    /// <param name="pageReader">Bitbucket pull request page reader.</param>
    /// <param name="snapshotMapper">Pull request snapshot mapper.</param>
    /// <param name="cacheService">Pull request details cache service.</param>
    /// <param name="activitySummaryProvider">Pull request activity summary provider.</param>
    /// <param name="domainFactory">Pull request domain model factory.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public BitbucketOpenPullRequestService(
        IBitbucketTransport transport,
        IBitbucketPullRequestUrlBuilder urlBuilder,
        IBitbucketPullRequestPageReader pageReader,
        IPullRequestSnapshotMapper snapshotMapper,
        IPullRequestDetailsCacheService cacheService,
        IPullRequestActivitySummaryProvider activitySummaryProvider,
        IPullRequestDomainFactory domainFactory,
        IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(urlBuilder);
        ArgumentNullException.ThrowIfNull(pageReader);
        ArgumentNullException.ThrowIfNull(snapshotMapper);
        ArgumentNullException.ThrowIfNull(cacheService);
        ArgumentNullException.ThrowIfNull(activitySummaryProvider);
        ArgumentNullException.ThrowIfNull(domainFactory);
        ArgumentNullException.ThrowIfNull(options);

        _transport = transport;
        _urlBuilder = urlBuilder;
        _pageReader = pageReader;
        _snapshotMapper = snapshotMapper;
        _cacheService = cacheService;
        _activitySummaryProvider = activitySummaryProvider;
        _domainFactory = domainFactory;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task PopulateOpenPullRequestCountAsync(Repository repository, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (!repository.CanPopulateOpenPullRequestsCount)
        {
            return;
        }

        var repositorySlug = repository.Slug!.Value;

        try
        {
            var url = _urlBuilder.CreateOpenPullRequestCountUrl(repositorySlug);
            var summary = await _transport.GetAsync<PullRequestPageSummaryDto>(url, cancellationToken).ConfigureAwait(false);
            repository.UpdateOpenPullRequestsCount(summary?.Size ?? 0);
        }
        catch (HttpRequestException)
        {
            return;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PullRequestDetail>> GetOpenPullRequestDetailsAsync(
        Repository repository,
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
        var details = new List<PullRequestDetail>();

        try
        {
            var cacheEntriesByPullRequestId = await _cacheService
                .ReadEntriesByPullRequestIdAsync(
                    workspace,
                    repositorySlug,
                    currentUserId,
                    PullRequestDetailsCacheScope.Open,
                    cancellationToken)
                .ConfigureAwait(false);

            var openPullRequests = await GetPullRequestSnapshotsAsync(
                repositorySlug,
                currentUserId,
                cancellationToken).ConfigureAwait(false);
            repository.UpdateOpenPullRequestsCount(openPullRequests.Count);

            if (openPullRequests.Count == 0)
            {
                await _cacheService
                    .DeleteAsync(
                        workspace,
                        repositorySlug,
                        currentUserId,
                        PullRequestDetailsCacheScope.Open,
                        cancellationToken)
                    .ConfigureAwait(false);
                return [];
            }

            var updatedCacheEntries = new List<PullRequestDetailsCacheEntry>(openPullRequests.Count);

            foreach (var pullRequest in openPullRequests)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var activitySummary = await _activitySummaryProvider
                    .GetAsync(
                        repositorySlug,
                        pullRequest,
                        cacheEntriesByPullRequestId,
                        currentUserId,
                        cancellationToken)
                    .ConfigureAwait(false);

                details.Add(_domainFactory.CreateDetail(repository, pullRequest, activitySummary.Summary));
                updatedCacheEntries.Add(activitySummary.CacheEntry);
            }

            await _cacheService
                .SaveEntriesAsync(
                    workspace,
                    repositorySlug,
                    currentUserId,
                    PullRequestDetailsCacheScope.Open,
                    updatedCacheEntries,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return [];
        }

        return details;
    }

    private async Task<IReadOnlyList<PullRequestSnapshot>> GetPullRequestSnapshotsAsync(
        RepositorySlug repositorySlug,
        BitbucketId currentUserId,
        CancellationToken cancellationToken)
    {
        var url = _urlBuilder.CreateOpenPullRequestSnapshotsUrl(repositorySlug);
        var pullRequests = new List<PullRequestSnapshot>();

        await _pageReader.ForEachAsync(
            url,
            (pullRequestDto, _) =>
            {
                if (pullRequestDto.Id is null || pullRequestDto.Id <= 0 || pullRequestDto.CreatedOn is null)
                {
                    return ValueTask.FromResult(true);
                }

                pullRequests.Add(_snapshotMapper.CreateSnapshot(pullRequestDto, currentUserId));
                return ValueTask.FromResult(true);
            },
            cancellationToken).ConfigureAwait(false);

        return pullRequests;
    }

    private readonly IBitbucketTransport _transport;
    private readonly IBitbucketPullRequestUrlBuilder _urlBuilder;
    private readonly IBitbucketPullRequestPageReader _pageReader;
    private readonly IPullRequestSnapshotMapper _snapshotMapper;
    private readonly IPullRequestDetailsCacheService _cacheService;
    private readonly IPullRequestActivitySummaryProvider _activitySummaryProvider;
    private readonly IPullRequestDomainFactory _domainFactory;
    private readonly BitbucketOptions _options;
}
