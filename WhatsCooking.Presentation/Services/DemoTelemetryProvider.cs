using System.Diagnostics.CodeAnalysis;

using BBRepoList.Models;

namespace WhatsCooking.Services;

/// <summary>
/// Creates synthetic Bitbucket telemetry for demo mode.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Provider is created by dependency injection.")]
internal sealed class DemoTelemetryProvider : IDemoTelemetryProvider
{
    /// <summary>
    /// Creates demo telemetry data.
    /// </summary>
    /// <returns>Demo telemetry snapshot.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Provider is resolved through dependency injection for consistency with other dashboard data sources.")]
    public BitbucketTelemetrySnapshot Create()
    {
        List<BitbucketApiRequestStatistic> statistics =
        [
            new("repositories/{workspace}", 12),
            new("user", 1),
            new("pullrequests?state=OPEN", 10),
            new("pullrequests?state=MERGED", 10),
            new("pullrequests/{id}/activity", 24),
            new("pullrequests?fields=size", 10),
            new("repositories/{workspace}/{repo}", 6),
            new("pullrequests/{id}/participants", 8),
            new("pullrequests/{id}/diffstat", 4),
            new("pullrequests/{id}/comments", 7)
        ];
        var totalRequests = statistics.Sum(static statistic => statistic.RequestCount);

        return new BitbucketTelemetrySnapshot(true, totalRequests, statistics);
    }
}
