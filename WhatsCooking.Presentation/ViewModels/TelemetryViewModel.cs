using System.Diagnostics.CodeAnalysis;

using BBRepoList.Abstractions;
using BBRepoList.Models;

using WhatsCooking.Services;

namespace WhatsCooking.ViewModels;

/// <summary>
/// View model for Bitbucket API telemetry table state.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "View model is created by dependency injection.")]
internal sealed class TelemetryViewModel : ObservableObject, ITelemetryDashboard, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryViewModel"/> class.
    /// </summary>
    /// <param name="telemetryService">Bitbucket API telemetry service.</param>
    /// <param name="filterDebouncer">Debouncer used for telemetry filters.</param>
    public TelemetryViewModel(
        IBitbucketTelemetryService telemetryService,
        IDebouncer filterDebouncer)
    {
        ArgumentNullException.ThrowIfNull(telemetryService);
        ArgumentNullException.ThrowIfNull(filterDebouncer);

        _telemetryService = telemetryService;
        _filterDebouncer = filterDebouncer;
    }

    /// <summary>
    /// Text filter applied to the telemetry table.
    /// </summary>
    public string TelemetryFilter {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                ScheduleFilterRefresh();
            }
        }
    } = string.Empty;

    /// <summary>
    /// Number of Bitbucket API requests captured by telemetry.
    /// </summary>
    public int TelemetryRequestsCount {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Number of Bitbucket API endpoints captured by telemetry.
    /// </summary>
    public int TelemetryEndpointsCount {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Whether Bitbucket API telemetry is enabled.
    /// </summary>
    public bool IsTelemetryEnabled {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Loaded telemetry rows.
    /// </summary>
    public BulkObservableCollection<TelemetryRow> TelemetryView { get; } = [];

    /// <summary>
    /// Reloads telemetry from the telemetry service.
    /// </summary>
    /// <returns>Loaded telemetry snapshot.</returns>
    public BitbucketTelemetrySnapshot RefreshTelemetry()
    {
        var snapshot = _telemetryService.GetSnapshot();
        LoadTelemetry(snapshot);
        return snapshot;
    }

    /// <summary>
    /// Applies telemetry snapshot to the view model.
    /// </summary>
    /// <param name="snapshot">Telemetry snapshot.</param>
    public void LoadTelemetry(BitbucketTelemetrySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        IsTelemetryEnabled = snapshot.IsEnabled;
        TelemetryRequestsCount = snapshot.TotalRequests;
        TelemetryEndpointsCount = snapshot.RequestStatistics.Count;
        _telemetryRows.Clear();
        for (var i = 0; i < snapshot.RequestStatistics.Count; i++)
        {
            _telemetryRows.Add(new TelemetryRow(i + 1, snapshot.RequestStatistics[i], snapshot.TotalRequests));
        }
        RefreshFilter();
    }

    /// <summary>
    /// Clears telemetry filter state.
    /// </summary>
    public void ResetFilter()
    {
        TelemetryFilter = string.Empty;
    }

    /// <summary>
    /// Releases resources held by the view model.
    /// </summary>
    public void Dispose()
    {
        _filterDebouncer.Dispose();
    }

    private void ScheduleFilterRefresh() =>
        _filterDebouncer.Schedule(RefreshFilter, _filterRefreshDelay);

    private void RefreshFilter() =>
        TelemetryView.ReplaceAll(_telemetryRows.Where(row => Matches(row.SearchText, TelemetryFilter)));

    private static bool Matches(string source, string filter) => string.IsNullOrWhiteSpace(filter) || source.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);

    private static readonly TimeSpan _filterRefreshDelay = TimeSpan.FromMilliseconds(150);

    private readonly IBitbucketTelemetryService _telemetryService;

    private readonly IDebouncer _filterDebouncer;

    private readonly List<TelemetryRow> _telemetryRows = [];
}
