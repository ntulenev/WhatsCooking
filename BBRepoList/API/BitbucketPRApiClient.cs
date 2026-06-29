using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Transport;

using Microsoft.Extensions.Options;

namespace BBRepoList.API;

/// <summary>
/// Bitbucket REST API client implementation for pull request operations.
/// </summary>
public sealed class BitbucketPRApiClient : IBitbucketPRApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketPRApiClient"/> class.
    /// </summary>
    /// <param name="transport">Bitbucket transport instance.</param>
    /// <param name="urlBuilder">Bitbucket pull request URL builder.</param>
    /// <param name="pageReader">Bitbucket pull request page reader.</param>
    /// <param name="activityAnalyzer">Pull request activity analyzer.</param>
    /// <param name="activityLoader">Pull request activity loader.</param>
    /// <param name="snapshotMapper">Pull request snapshot mapper.</param>
    /// <param name="cacheService">Pull request details cache service.</param>
    /// <param name="telemetryService">Bitbucket telemetry service.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public BitbucketPRApiClient(
        IBitbucketTransport transport,
        IBitbucketPullRequestUrlBuilder urlBuilder,
        IBitbucketPullRequestPageReader pageReader,
        IPullRequestActivityAnalyzer activityAnalyzer,
        IBitbucketPullRequestActivityLoader activityLoader,
        IPullRequestSnapshotMapper snapshotMapper,
        IPullRequestDetailsCacheService cacheService,
        IBitbucketTelemetryService telemetryService,
        IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(urlBuilder);
        ArgumentNullException.ThrowIfNull(pageReader);
        ArgumentNullException.ThrowIfNull(activityAnalyzer);
        ArgumentNullException.ThrowIfNull(activityLoader);
        ArgumentNullException.ThrowIfNull(snapshotMapper);
        ArgumentNullException.ThrowIfNull(cacheService);
        ArgumentNullException.ThrowIfNull(telemetryService);
        ArgumentNullException.ThrowIfNull(options);

        _transport = transport;
        _urlBuilder = urlBuilder;
        _pageReader = pageReader;
        _activityAnalyzer = activityAnalyzer;
        _activityLoader = activityLoader;
        _snapshotMapper = snapshotMapper;
        _cacheService = cacheService;
        _telemetryService = telemetryService;
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

                if (TryCreateDetailFromCache(
                    repository,
                    pullRequest,
                    cacheEntriesByPullRequestId,
                    out var cachedDetail,
                    out var cacheEntry))
                {
                    _telemetryService.TrackCacheHit();
                    details.Add(cachedDetail);
                    updatedCacheEntries.Add(cacheEntry);
                    continue;
                }

                _telemetryService.TrackCacheMiss();
                var activities = await _activityLoader.GetActivitiesAsync(
                    repositorySlug,
                    pullRequest.Id,
                    cancellationToken).ConfigureAwait(false);
                var activitySummary = _activityAnalyzer.CreateSummary(activities, pullRequest, currentUserId);

                details.Add(CreatePullRequestDetail(repository, pullRequest, activitySummary));
                updatedCacheEntries.Add(_cacheService.CreateEntry(pullRequest, activitySummary));
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
                    PullRequestActivitySummary activitySummary;
                    PullRequestDetailsCacheEntry cacheEntry;

                    if (!_cacheService.TryCreateActivitySummary(
                        pullRequest,
                        cacheEntriesByPullRequestId,
                        out activitySummary,
                        out cacheEntry))
                    {
                        _telemetryService.TrackCacheMiss();
                        var activities = await _activityLoader.GetActivitiesAsync(
                            repositorySlug,
                            pullRequest.Id,
                            token).ConfigureAwait(false);
                        activitySummary = _activityAnalyzer.CreateSummary(activities, pullRequest, currentUserId);
                        cacheEntry = _cacheService.CreateEntry(pullRequest, activitySummary);
                    }
                    else
                    {
                        _telemetryService.TrackCacheHit();
                    }

                    pullRequests.Add(CreateMergedPullRequest(repository, pullRequest, pullRequestDto.UpdatedOn.Value, activitySummary));
                    updatedCacheEntries.Add(cacheEntry);
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

    private static PullRequestDetail CreatePullRequestDetail(
        Repository repository,
        PullRequestSnapshot pullRequest,
        PullRequestActivitySummary activitySummary) =>
        new(
            repository,
            pullRequest.Id,
            pullRequest.Title,
            pullRequest.CreatedOn,
            pullRequest.AuthorId,
            pullRequest.AuthorDisplayName,
            activitySummary.FirstNonAuthorActivityOn,
            activitySummary.LastActivityOn,
            activitySummary.HasCurrentUserDiscussion,
            pullRequest.DescriptionText,
            activitySummary.CommentsCount,
            pullRequest.RequestChangesCount,
            pullRequest.HasCurrentUserRequestChanges,
            pullRequest.ApprovalsCount,
            pullRequest.HasCurrentUserApproval);

    private static MergedPullRequest CreateMergedPullRequest(
        Repository repository,
        PullRequestSnapshot pullRequest,
        DateTimeOffset mergedOn,
        PullRequestActivitySummary activitySummary) =>
        new(
            repository,
            pullRequest.Id,
            pullRequest.Title,
            pullRequest.CreatedOn,
            pullRequest.AuthorId,
            pullRequest.AuthorDisplayName,
            activitySummary.FirstNonAuthorActivityOn,
            activitySummary.LastActivityOn,
            activitySummary.HasCurrentUserDiscussion,
            mergedOn,
            pullRequest.DescriptionText,
            activitySummary.CommentsCount,
            pullRequest.RequestChangesCount,
            pullRequest.HasCurrentUserRequestChanges,
            pullRequest.ApprovalsCount,
            pullRequest.HasCurrentUserApproval);

    private bool TryCreateDetailFromCache(
        Repository repository,
        PullRequestSnapshot pullRequest,
        IReadOnlyDictionary<PullRequestId, PullRequestDetailsCacheEntry> cacheEntriesByPullRequestId,
        out PullRequestDetail detail,
        out PullRequestDetailsCacheEntry cacheEntry)
    {
        detail = null!;

        if (!_cacheService.TryCreateActivitySummary(
            pullRequest,
            cacheEntriesByPullRequestId,
            out var activitySummary,
            out cacheEntry))
        {
            return false;
        }

        detail = CreatePullRequestDetail(repository, pullRequest, activitySummary);
        return true;
    }

    private readonly IBitbucketTransport _transport;
    private readonly IBitbucketPullRequestUrlBuilder _urlBuilder;
    private readonly IBitbucketPullRequestPageReader _pageReader;
    private readonly IPullRequestActivityAnalyzer _activityAnalyzer;
    private readonly IBitbucketPullRequestActivityLoader _activityLoader;
    private readonly IPullRequestSnapshotMapper _snapshotMapper;
    private readonly IPullRequestDetailsCacheService _cacheService;
    private readonly IBitbucketTelemetryService _telemetryService;
    private readonly BitbucketOptions _options;
}
