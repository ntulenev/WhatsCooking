using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using BBRepoList.Abstractions;
using BBRepoList.Models;

using WhatsCooking.Services;

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
    /// <param name="cache">Pull request details cache.</param>
    /// <param name="dialogService">User-facing dialog service.</param>
    /// <param name="filterDebouncer">Debouncer used for telemetry filters.</param>
    public TelemetryViewModel(
        IBitbucketTelemetryService telemetryService,
        IPullRequestDetailsCache cache,
        IDialogService dialogService,
        IDebouncer filterDebouncer)
    {
        ArgumentNullException.ThrowIfNull(telemetryService);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(filterDebouncer);

        _telemetryService = telemetryService;
        _cache = cache;
        _dialogService = dialogService;
        _filterDebouncer = filterDebouncer;
        ClearCacheCommand = new RelayCommand(ClearCache);
        RefreshCacheSize();
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
    /// Number of pull request activity summaries loaded from cache.
    /// </summary>
    public int CacheHits {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Number of pull request activity summaries loaded from the API after a cache miss.
    /// </summary>
    public int CacheMisses {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Formatted cache hit rate for pull request activity summaries.
    /// </summary>
    public string CacheHitRate {
        get;
        private set => SetProperty(ref field, value);
    } = "0.0 %";

    /// <summary>
    /// Formatted persisted pull request details cache size.
    /// </summary>
    public string CacheSize {
        get;
        private set => SetProperty(ref field, value);
    } = "0 B";

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
    /// Command that clears persisted pull request details cache.
    /// </summary>
    public RelayCommand ClearCacheCommand { get; }

    /// <summary>
    /// Reloads telemetry from the telemetry service.
    /// </summary>
    /// <returns>Loaded telemetry snapshot.</returns>
    public BitbucketTelemetrySnapshot RefreshTelemetry()
    {
        var snapshot = _telemetryService.GetSnapshot();
        LoadTelemetry(snapshot);
        RefreshCacheSize();
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
        CacheHits = snapshot.CacheHits;
        CacheMisses = snapshot.CacheMisses;
        var cacheLookups = snapshot.CacheHits + snapshot.CacheMisses;
        CacheHitRate = (cacheLookups > 0 ? (double)snapshot.CacheHits / cacheLookups : 0)
            .ToString("P1", System.Globalization.CultureInfo.InvariantCulture);
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

    private void ClearCache()
    {
        if (!_dialogService.ConfirmClearCache())
        {
            return;
        }

        _cache.Clear();
        RefreshCacheSize();
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

    private void RefreshCacheSize() =>
        CacheSize = FormatSize(_cache.GetSizeInBytes());

    private static string FormatSize(long sizeInBytes)
    {
        if (sizeInBytes <= 0)
        {
            return "0 B";
        }

        string[] units = ["B", "KB", "MB", "GB"];
        var unitIndex = 0;
        var size = (double)sizeInBytes;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? string.Create(CultureInfo.InvariantCulture, $"{sizeInBytes} {units[unitIndex]}")
            : string.Create(CultureInfo.InvariantCulture, $"{size:0.0} {units[unitIndex]}");
    }

    private static bool Matches(string source, string filter) => string.IsNullOrWhiteSpace(filter) || source.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);

    private static readonly TimeSpan _filterRefreshDelay = TimeSpan.FromMilliseconds(150);

    private readonly IBitbucketTelemetryService _telemetryService;

    private readonly IPullRequestDetailsCache _cache;

    private readonly IDialogService _dialogService;

    private readonly IDebouncer _filterDebouncer;

    private readonly List<TelemetryRow> _telemetryRows = [];
}
