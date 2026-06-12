namespace BBRepoList.Models;

/// <summary>
/// Bitbucket telemetry snapshot for the current run.
/// </summary>
/// <param name="IsEnabled">Whether telemetry is enabled.</param>
/// <param name="TotalRequests">Total number of tracked Bitbucket API requests.</param>
/// <param name="RequestStatistics">Aggregated request statistics by API.</param>
/// <param name="CacheHits">Number of pull request activity summaries loaded from cache.</param>
/// <param name="CacheMisses">Number of pull request activity summaries loaded from the API after a cache miss.</param>
public sealed record BitbucketTelemetrySnapshot(
    bool IsEnabled,
    int TotalRequests,
    IReadOnlyList<BitbucketApiRequestStatistic> RequestStatistics,
    int CacheHits = 0,
    int CacheMisses = 0);
