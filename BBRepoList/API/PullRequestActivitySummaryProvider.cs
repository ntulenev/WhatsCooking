using BBRepoList.Abstractions;
using BBRepoList.Models;

namespace BBRepoList.API;

/// <summary>
/// Resolves pull request activity summaries from cache before loading Bitbucket activity data.
/// </summary>
public sealed class PullRequestActivitySummaryProvider : IPullRequestActivitySummaryProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestActivitySummaryProvider"/> class.
    /// </summary>
    /// <param name="activityAnalyzer">Pull request activity analyzer.</param>
    /// <param name="activityLoader">Pull request activity loader.</param>
    /// <param name="cacheService">Pull request details cache service.</param>
    /// <param name="telemetryService">Bitbucket telemetry service.</param>
    public PullRequestActivitySummaryProvider(
        IPullRequestActivityAnalyzer activityAnalyzer,
        IBitbucketPullRequestActivityLoader activityLoader,
        IPullRequestDetailsCacheService cacheService,
        IBitbucketTelemetryService telemetryService)
    {
        ArgumentNullException.ThrowIfNull(activityAnalyzer);
        ArgumentNullException.ThrowIfNull(activityLoader);
        ArgumentNullException.ThrowIfNull(cacheService);
        ArgumentNullException.ThrowIfNull(telemetryService);

        _activityAnalyzer = activityAnalyzer;
        _activityLoader = activityLoader;
        _cacheService = cacheService;
        _telemetryService = telemetryService;
    }

    /// <inheritdoc />
    public async Task<PullRequestActivitySummaryResult> GetAsync(
        RepositorySlug repositorySlug,
        PullRequestSnapshot pullRequest,
        IReadOnlyDictionary<PullRequestId, PullRequestDetailsCacheEntry> cachedEntries,
        BitbucketId currentUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cachedEntries);

        if (_cacheService.TryCreateActivitySummary(
            pullRequest,
            cachedEntries,
            out var cachedSummary,
            out var cachedEntry))
        {
            _telemetryService.TrackCacheHit();
            return new PullRequestActivitySummaryResult(cachedSummary, cachedEntry);
        }

        _telemetryService.TrackCacheMiss();
        var activities = await _activityLoader.GetActivitiesAsync(
            repositorySlug,
            pullRequest.Id,
            cancellationToken).ConfigureAwait(false);
        var activitySummary = _activityAnalyzer.CreateSummary(activities, pullRequest, currentUserId);
        var cacheEntry = _cacheService.CreateEntry(pullRequest, activitySummary);

        return new PullRequestActivitySummaryResult(activitySummary, cacheEntry);
    }

    private readonly IPullRequestActivityAnalyzer _activityAnalyzer;
    private readonly IBitbucketPullRequestActivityLoader _activityLoader;
    private readonly IPullRequestDetailsCacheService _cacheService;
    private readonly IBitbucketTelemetryService _telemetryService;
}
