using System.Collections.ObjectModel;
using System.ComponentModel;

using BBRepoList.Models;

namespace WhatsCooking.ViewModels;

/// <summary>
/// View model contract for Bitbucket API telemetry table state.
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
    /// Whether Bitbucket API telemetry is enabled.
    /// </summary>
    bool IsTelemetryEnabled { get; }

    /// <summary>
    /// Loaded telemetry rows.
    /// </summary>
    ObservableCollection<TelemetryRow> Telemetry { get; }

    /// <summary>
    /// Filterable view over telemetry rows.
    /// </summary>
    ICollectionView TelemetryView { get; }

    /// <summary>
    /// Reloads telemetry from the telemetry service.
    /// </summary>
    /// <returns>Loaded telemetry snapshot.</returns>
    BitbucketTelemetrySnapshot RefreshTelemetry();

    /// <summary>
    /// Applies telemetry snapshot to the view model.
    /// </summary>
    /// <param name="snapshot">Telemetry snapshot.</param>
    void LoadTelemetry(BitbucketTelemetrySnapshot snapshot);

    /// <summary>
    /// Clears telemetry filter state.
    /// </summary>
    void ResetFilter();
}
