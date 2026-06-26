using System.Windows.Input;

using BBRepoList.Models;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Exposes telemetry dashboard state and commands used by the main dashboard.
/// </summary>
internal interface ITelemetryDashboard
{
    /// <summary>
    /// Text filter applied to the telemetry table.
    /// </summary>
    string TelemetryFilter { get; set; }

    /// <summary>
    /// Number of Bitbucket API requests captured by telemetry.
    /// </summary>
    int TelemetryRequestsCount { get; }

    /// <summary>
    /// Number of Bitbucket API endpoints captured by telemetry.
    /// </summary>
    int TelemetryEndpointsCount { get; }

    /// <summary>
    /// Number of pull request activity summaries loaded from cache.
    /// </summary>
    int CacheHits { get; }

    /// <summary>
    /// Number of pull request activity summaries loaded from the API after a cache miss.
    /// </summary>
    int CacheMisses { get; }

    /// <summary>
    /// Formatted cache hit rate for pull request activity summaries.
    /// </summary>
    string CacheHitRate { get; }

    /// <summary>
    /// Formatted persisted pull request details cache size.
    /// </summary>
    string CacheSize { get; }

    /// <summary>
    /// Whether Bitbucket API telemetry is enabled.
    /// </summary>
    bool IsTelemetryEnabled { get; }

    /// <summary>
    /// Loaded telemetry rows.
    /// </summary>
    BulkObservableCollection<TelemetryRow> TelemetryView { get; }

    /// <summary>
    /// Command that clears persisted pull request details cache.
    /// </summary>
    ICommand ClearCacheCommand { get; }

    /// <summary>
    /// Reloads telemetry from the telemetry service.
    /// </summary>
    /// <returns>Loaded telemetry snapshot.</returns>
    BitbucketTelemetrySnapshot RefreshTelemetry();

    /// <summary>
    /// Applies telemetry snapshot to the dashboard state.
    /// </summary>
    /// <param name="snapshot">Telemetry snapshot.</param>
    void LoadTelemetry(BitbucketTelemetrySnapshot snapshot);

    /// <summary>
    /// Clears telemetry filter state.
    /// </summary>
    void ResetFilter();
}
