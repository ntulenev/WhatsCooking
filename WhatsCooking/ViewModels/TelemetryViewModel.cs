using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Data;
using System.Windows.Threading;

using BBRepoList.Abstractions;
using BBRepoList.Models;

namespace WhatsCooking.ViewModels;

/// <summary>
/// View model for Bitbucket API telemetry table state.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "View model is created by dependency injection.")]
internal sealed class TelemetryViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryViewModel"/> class.
    /// </summary>
    /// <param name="telemetryService">Bitbucket API telemetry service.</param>
    public TelemetryViewModel(IBitbucketTelemetryService telemetryService)
    {
        ArgumentNullException.ThrowIfNull(telemetryService);

        _telemetryService = telemetryService;
        TelemetryView = CollectionViewSource.GetDefaultView(Telemetry);
        TelemetryView.Filter = FilterTelemetryRow;
        _filterRefreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = _filterRefreshDelay
        };
        _filterRefreshTimer.Tick += OnFilterRefreshTimerTick;
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
    public ObservableCollection<TelemetryRow> Telemetry { get; } = [];

    /// <summary>
    /// Filterable view over telemetry rows.
    /// </summary>
    public ICollectionView TelemetryView { get; }

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
        Telemetry.Clear();
        for (var i = 0; i < snapshot.RequestStatistics.Count; i++)
        {
            Telemetry.Add(new TelemetryRow(i + 1, snapshot.RequestStatistics[i], snapshot.TotalRequests));
        }
        TelemetryView.Refresh();
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
        _filterRefreshTimer.Stop();
    }

    private bool FilterTelemetryRow(object item) => item is TelemetryRow row && Matches(row.SearchText, TelemetryFilter);

    private void ScheduleFilterRefresh()
    {
        _filterRefreshTimer.Stop();
        _filterRefreshTimer.Start();
    }

    private void OnFilterRefreshTimerTick(object? sender, EventArgs e)
    {
        _filterRefreshTimer.Stop();
        TelemetryView.Refresh();
    }

    private static bool Matches(string source, string filter) => string.IsNullOrWhiteSpace(filter) || source.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);

    private static readonly TimeSpan _filterRefreshDelay = TimeSpan.FromMilliseconds(150);

    private readonly IBitbucketTelemetryService _telemetryService;

    private readonly DispatcherTimer _filterRefreshTimer;
}
