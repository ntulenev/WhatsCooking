using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

using WhatsCooking.Services;

namespace WhatsCooking.ViewModels;

/// <summary>
/// View model for the pull request dashboard window.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "View model is created by dependency injection.")]
internal sealed class MainViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="loader">Pull request dashboard data loader.</param>
    /// <param name="telemetryService">Bitbucket API telemetry service.</param>
    /// <param name="demoDataProvider">Demo dashboard data provider.</param>
    /// <param name="demoTelemetryProvider">Demo telemetry provider.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    /// <param name="preferencesService">User preferences persistence service.</param>
    public MainViewModel(
        PullRequestDashboardLoader loader,
        IBitbucketTelemetryService telemetryService,
        DemoPullRequestDashboardProvider demoDataProvider,
        DemoTelemetryProvider demoTelemetryProvider,
        IOptions<BitbucketOptions> options,
        UserPreferencesService preferencesService)
    {
        ArgumentNullException.ThrowIfNull(loader, nameof(loader));
        ArgumentNullException.ThrowIfNull(telemetryService, nameof(telemetryService));
        ArgumentNullException.ThrowIfNull(demoDataProvider, nameof(demoDataProvider));
        ArgumentNullException.ThrowIfNull(demoTelemetryProvider, nameof(demoTelemetryProvider));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(preferencesService, nameof(preferencesService));
        _loader = loader;
        _telemetryService = telemetryService;
        _demoDataProvider = demoDataProvider;
        _demoTelemetryProvider = demoTelemetryProvider;
        _options = options.Value;
        _preferencesService = preferencesService;
        _preferences = _preferencesService.Load();
        _isLightTheme = _preferences.IsLightTheme;
        _uiScale = NormalizeUiScale(_preferences.UiScale);
        _selectedSearchMode = _preferences.SearchMode ?? RepositorySearchMode.StartWith;
        _searchPhrase = _preferences.SearchPhrase ?? string.Empty;
        _mergedPullRequestsDays = 1;
        OpenPullRequestFilters = new PullRequestFilterState(SchedulePullRequestFilterRefresh);
        MergedPullRequestFilters = new PullRequestFilterState(SchedulePullRequestFilterRefresh);
        OpenPullRequestsView = CollectionViewSource.GetDefaultView(OpenPullRequests);
        MergedPullRequestsView = CollectionViewSource.GetDefaultView(MergedPullRequests);
        TelemetryView = CollectionViewSource.GetDefaultView(Telemetry);
        _pullRequestFilterRefreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = _filterRefreshDelay
        };
        _pullRequestFilterRefreshTimer.Tick += OnPullRequestFilterRefreshTimerTick;
        _telemetryFilterRefreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = _filterRefreshDelay
        };
        _telemetryFilterRefreshTimer.Tick += OnTelemetryFilterRefreshTimerTick;
        OpenPullRequestsView.Filter = FilterOpenPullRequestRow;
        MergedPullRequestsView.Filter = FilterMergedPullRequestRow;
        TelemetryView.Filter = FilterTelemetryRow;
        LoadCommand = new RelayCommand(async () => await LoadAsync().ConfigureAwait(false), CanLoad);
        CancelCommand = new RelayCommand(Cancel, () => IsLoading);
        OpenUrlCommand = new RelayCommand(OpenUrl);
        ResetFiltersCommand = new RelayCommand(ResetFilters);
        IncreaseUiScaleCommand = new RelayCommand(IncreaseUiScale);
        DecreaseUiScaleCommand = new RelayCommand(DecreaseUiScale);
        RefreshTelemetry();
    }

    /// <summary>
    /// Repository search modes available in the UI.
    /// </summary>
    public static Array SearchModes => Enum.GetValues<RepositorySearchMode>();

    /// <summary>
    /// Selected repository search mode.
    /// </summary>
    public RepositorySearchMode SelectedSearchMode {
        get => _selectedSearchMode;
        set => SetProperty(ref _selectedSearchMode, value);
    }

    /// <summary>
    /// Repository search phrase entered by the user.
    /// </summary>
    public string SearchPhrase {
        get => _searchPhrase;
        set => SetProperty(ref _searchPhrase, value ?? string.Empty);
    }

    /// <summary>
    /// Number of days used to load recently merged pull requests.
    /// </summary>
    public int MergedPullRequestsDays {
        get => _mergedPullRequestsDays;
        set
        {
            if (SetProperty(ref _mergedPullRequestsDays, Math.Max(value, 1)))
            {
                RaiseCommandStates();
            }
        }
    }

    /// <summary>
    /// Current application status text.
    /// </summary>
    public string Status {
        get;
        private set => SetProperty(ref field, value);
    } = "Ready";

    /// <summary>
    /// Gets or sets a value indicating whether the light UI theme is enabled.
    /// </summary>
    public bool IsLightTheme {
        get => _isLightTheme;
        set
        {
            if (SetProperty(ref _isLightTheme, value))
            {
                _preferences.IsLightTheme = value;
                _preferencesService.Save(_preferences);
            }
        }
    }

    /// <summary>
    /// UI scale multiplier.
    /// </summary>
    public double UiScale {
        get => _uiScale;
        private set
        {
            var normalizedValue = NormalizeUiScale(value);
            if (SetProperty(ref _uiScale, normalizedValue))
            {
                _preferences.UiScale = normalizedValue;
                _preferencesService.Save(_preferences);
            }
        }
    }

    /// <summary>
    /// Global text filter applied to pull request tables.
    /// </summary>
    public string GlobalSearch {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                SchedulePullRequestFilterRefresh();
            }
        }
    } = string.Empty;

    /// <summary>
    /// Filter for the open pull request row number column.
    /// </summary>
    public string OpenNumberFilter {
        get => OpenPullRequestFilters.Number;
        set => SetOpenPullRequestFilter(value, static (filters, filter) => filters.Number = filter);
    }

    /// <summary>
    /// Filter for the open pull request repository column.
    /// </summary>
    public string OpenRepositoryFilter {
        get => OpenPullRequestFilters.Repository;
        set => SetOpenPullRequestFilter(value, static (filters, filter) => filters.Repository = filter);
    }

    /// <summary>
    /// Filter for the open pull request column.
    /// </summary>
    public string OpenPullRequestFilter {
        get => OpenPullRequestFilters.PullRequest;
        set => SetOpenPullRequestFilter(value, static (filters, filter) => filters.PullRequest = filter);
    }

    /// <summary>
    /// Filter for the open pull request author column.
    /// </summary>
    public string OpenAuthorFilter {
        get => OpenPullRequestFilters.Author;
        set => SetOpenPullRequestFilter(value, static (filters, filter) => filters.Author = filter);
    }

    /// <summary>
    /// Filter for the open pull request description length column.
    /// </summary>
    public string OpenDescriptionLengthFilter {
        get => OpenPullRequestFilters.DescriptionLength;
        set => SetOpenPullRequestFilter(value, static (filters, filter) => filters.DescriptionLength = filter);
    }

    /// <summary>
    /// Filter for the open pull request open duration column.
    /// </summary>
    public string OpenOpenForFilter {
        get => OpenPullRequestFilters.OpenFor;
        set => SetOpenPullRequestFilter(value, static (filters, filter) => filters.OpenFor = filter);
    }

    /// <summary>
    /// Filter for the open pull request TTFR column.
    /// </summary>
    public string OpenTimeToFirstResponseFilter {
        get => OpenPullRequestFilters.TimeToFirstResponse;
        set => SetOpenPullRequestFilter(value, static (filters, filter) => filters.TimeToFirstResponse = filter);
    }

    /// <summary>
    /// Filter for the open pull request latest activity column.
    /// </summary>
    public string OpenActivityFilter {
        get => OpenPullRequestFilters.Activity;
        set => SetOpenPullRequestFilter(value, static (filters, filter) => filters.Activity = filter);
    }

    /// <summary>
    /// Filter for the open pull request comments column.
    /// </summary>
    public string OpenCommentsFilter {
        get => OpenPullRequestFilters.Comments;
        set => SetOpenPullRequestFilter(value, static (filters, filter) => filters.Comments = filter);
    }

    /// <summary>
    /// Filter for the open pull request changes column.
    /// </summary>
    public string OpenRequestChangesFilter {
        get => OpenPullRequestFilters.RequestChanges;
        set => SetOpenPullRequestFilter(value, static (filters, filter) => filters.RequestChanges = filter);
    }

    /// <summary>
    /// Filter for the open pull request approvals column.
    /// </summary>
    public string OpenApprovalsFilter {
        get => OpenPullRequestFilters.Approvals;
        set => SetOpenPullRequestFilter(value, static (filters, filter) => filters.Approvals = filter);
    }

    /// <summary>
    /// Filter for the open pull request current user activity column.
    /// </summary>
    public string OpenCurrentUserActivityFilter {
        get => OpenPullRequestFilters.CurrentUserActivity;
        set => SetOpenPullRequestFilter(value, static (filters, filter) => filters.CurrentUserActivity = filter);
    }

    /// <summary>
    /// Filter for the merged pull request row number column.
    /// </summary>
    public string MergedNumberFilter {
        get => MergedPullRequestFilters.Number;
        set => SetMergedPullRequestFilter(value, static (filters, filter) => filters.Number = filter);
    }

    /// <summary>
    /// Filter for the merged pull request repository column.
    /// </summary>
    public string MergedRepositoryFilter {
        get => MergedPullRequestFilters.Repository;
        set => SetMergedPullRequestFilter(value, static (filters, filter) => filters.Repository = filter);
    }

    /// <summary>
    /// Filter for the merged pull request column.
    /// </summary>
    public string MergedPullRequestFilter {
        get => MergedPullRequestFilters.PullRequest;
        set => SetMergedPullRequestFilter(value, static (filters, filter) => filters.PullRequest = filter);
    }

    /// <summary>
    /// Filter for the merged pull request author column.
    /// </summary>
    public string MergedAuthorFilter {
        get => MergedPullRequestFilters.Author;
        set => SetMergedPullRequestFilter(value, static (filters, filter) => filters.Author = filter);
    }

    /// <summary>
    /// Filter for the merged pull request description length column.
    /// </summary>
    public string MergedDescriptionLengthFilter {
        get => MergedPullRequestFilters.DescriptionLength;
        set => SetMergedPullRequestFilter(value, static (filters, filter) => filters.DescriptionLength = filter);
    }

    /// <summary>
    /// Filter for the merged pull request open duration column.
    /// </summary>
    public string MergedOpenForFilter {
        get => MergedPullRequestFilters.OpenFor;
        set => SetMergedPullRequestFilter(value, static (filters, filter) => filters.OpenFor = filter);
    }

    /// <summary>
    /// Filter for the merged pull request TTFR column.
    /// </summary>
    public string MergedTimeToFirstResponseFilter {
        get => MergedPullRequestFilters.TimeToFirstResponse;
        set => SetMergedPullRequestFilter(value, static (filters, filter) => filters.TimeToFirstResponse = filter);
    }

    /// <summary>
    /// Filter for the merged pull request merge age column.
    /// </summary>
    public string MergedActivityFilter {
        get => MergedPullRequestFilters.Activity;
        set => SetMergedPullRequestFilter(value, static (filters, filter) => filters.Activity = filter);
    }

    /// <summary>
    /// Filter for the merged pull request comments column.
    /// </summary>
    public string MergedCommentsFilter {
        get => MergedPullRequestFilters.Comments;
        set => SetMergedPullRequestFilter(value, static (filters, filter) => filters.Comments = filter);
    }

    /// <summary>
    /// Filter for the merged pull request changes column.
    /// </summary>
    public string MergedRequestChangesFilter {
        get => MergedPullRequestFilters.RequestChanges;
        set => SetMergedPullRequestFilter(value, static (filters, filter) => filters.RequestChanges = filter);
    }

    /// <summary>
    /// Filter for the merged pull request approvals column.
    /// </summary>
    public string MergedApprovalsFilter {
        get => MergedPullRequestFilters.Approvals;
        set => SetMergedPullRequestFilter(value, static (filters, filter) => filters.Approvals = filter);
    }

    /// <summary>
    /// Filter for the merged pull request current user activity column.
    /// </summary>
    public string MergedCurrentUserActivityFilter {
        get => MergedPullRequestFilters.CurrentUserActivity;
        set => SetMergedPullRequestFilter(value, static (filters, filter) => filters.CurrentUserActivity = filter);
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
                ScheduleTelemetryFilterRefresh();
            }
        }
    } = string.Empty;

    /// <summary>
    /// Whether pull request data is currently loading.
    /// </summary>
    public bool IsLoading {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                RaiseCommandStates();
            }
        }
    }

    /// <summary>
    /// Number of repositories matched by the current repository filter.
    /// </summary>
    public int RepositoriesCount {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Number of open pull requests loaded into the dashboard.
    /// </summary>
    public int OpenPullRequestsCount {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Number of recently merged pull requests loaded into the dashboard.
    /// </summary>
    public int MergedPullRequestsCount {
        get;
        private set => SetProperty(ref field, value);
    }

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
    /// Loaded open pull request rows.
    /// </summary>
    public ObservableCollection<PullRequestRow> OpenPullRequests { get; } = [];

    /// <summary>
    /// Loaded recently merged pull request rows.
    /// </summary>
    public ObservableCollection<PullRequestRow> MergedPullRequests { get; } = [];

    /// <summary>
    /// Loaded telemetry rows.
    /// </summary>
    public ObservableCollection<TelemetryRow> Telemetry { get; } = [];

    /// <summary>
    /// Open pull request grid filters.
    /// </summary>
    public PullRequestFilterState OpenPullRequestFilters { get; }

    /// <summary>
    /// Recently merged pull request grid filters.
    /// </summary>
    public PullRequestFilterState MergedPullRequestFilters { get; }

    /// <summary>
    /// Filterable view over open pull request rows.
    /// </summary>
    public ICollectionView OpenPullRequestsView { get; }

    /// <summary>
    /// Filterable view over recently merged pull request rows.
    /// </summary>
    public ICollectionView MergedPullRequestsView { get; }

    /// <summary>
    /// Filterable view over telemetry rows.
    /// </summary>
    public ICollectionView TelemetryView { get; }

    /// <summary>
    /// Command that loads pull request data from Bitbucket.
    /// </summary>
    public ICommand LoadCommand { get; }

    /// <summary>
    /// Command that cancels the current pull request loading operation.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Command that opens a Bitbucket URL in the default browser.
    /// </summary>
    public ICommand OpenUrlCommand { get; }

    /// <summary>
    /// Command that clears all table filters.
    /// </summary>
    public ICommand ResetFiltersCommand { get; }

    /// <summary>
    /// Command that increases the UI scale.
    /// </summary>
    public ICommand IncreaseUiScaleCommand { get; }

    /// <summary>
    /// Command that decreases the UI scale.
    /// </summary>
    public ICommand DecreaseUiScaleCommand { get; }

    /// <summary>
    /// Applies a pull request grid filter from a column header editor.
    /// </summary>
    /// <param name="scope">Pull request grid scope.</param>
    /// <param name="column">Pull request grid filter column.</param>
    /// <param name="value">Filter value.</param>
    public void ApplyPullRequestFilter(string scope, string column, string value)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(value);

        var filters = string.Equals(scope, "Merged", StringComparison.Ordinal)
            ? MergedPullRequestFilters
            : OpenPullRequestFilters;

        switch (column)
        {
            case "Number":
                filters.Number = value;
                break;
            case "Repository":
                filters.Repository = value;
                break;
            case "PullRequest":
                filters.PullRequest = value;
                break;
            case "Author":
                filters.Author = value;
                break;
            case "DescriptionLength":
                filters.DescriptionLength = value;
                break;
            case "OpenFor":
                filters.OpenFor = value;
                break;
            case "TimeToFirstResponse":
                filters.TimeToFirstResponse = value;
                break;
            case "Activity":
                filters.Activity = value;
                break;
            case "Comments":
                filters.Comments = value;
                break;
            case "RequestChanges":
                filters.RequestChanges = value;
                break;
            case "Approvals":
                filters.Approvals = value;
                break;
            case "CurrentUserActivity":
                filters.CurrentUserActivity = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(column), column, "Unknown pull request filter column.");
        }
    }

    private async Task LoadAsync()
    {
        if (!CanLoad())
        {
            return;
        }

        if (HasLoadedPullRequests() && !ConfirmReload())
        {
            return;
        }

        _loadCancellation = new CancellationTokenSource();
        IsLoading = true;
        Status = "Starting";
        try
        {
            SaveLoadPreferences();

            if (_options.DemoMode)
            {
                ApplyDashboardSnapshot(CreateDemoDashboardSnapshot());
                Status = $"Loaded demo data: {OpenPullRequestsCount} open PRs and {MergedPullRequestsCount} merged PRs";
                return;
            }

            var filterPattern = new FilterPattern(SearchPhrase, SelectedSearchMode);
            var progress = new Progress<PullRequestLoadProgress>(value =>
            {
                Status = value.Message;
                RefreshTelemetry();
            });
            var result = await _loader.LoadAsync(filterPattern, MergedPullRequestsDays, progress, _loadCancellation.Token).ConfigureAwait(true);
            ApplyDashboardSnapshot(new PullRequestDashboardSnapshot(
                DateTimeOffset.Now,
                result.Repositories,
                result.OpenPullRequests,
                result.MergedPullRequests,
                _telemetryService.GetSnapshot()));
            Status = $"Loaded {OpenPullRequestsCount} open PRs and {MergedPullRequestsCount} merged PRs";
        }
        catch (OperationCanceledException)
        {
            Status = "Cancelled";
        }
        catch (HttpRequestException ex)
        {
            ShowLoadError(ex);
        }
        catch (JsonException ex)
        {
            ShowLoadError(ex);
        }
        catch (InvalidOperationException ex)
        {
            ShowLoadError(ex);
        }
        catch (ArgumentException ex)
        {
            ShowLoadError(ex);
        }
        finally
        {
            _loadCancellation?.Dispose();
            _loadCancellation = null;
            IsLoading = false;
        }
    }

    private PullRequestDashboardSnapshot CreateDemoDashboardSnapshot()
    {
        Status = "Loading demo data";
        var asOf = DateTimeOffset.Now;
        var result = _demoDataProvider.Create();

        return new PullRequestDashboardSnapshot(
            asOf,
            result.Repositories,
            result.OpenPullRequests,
            result.MergedPullRequests,
            _demoTelemetryProvider.Create());
    }

    private void ApplyDashboardSnapshot(PullRequestDashboardSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        OpenPullRequests.Clear();
        MergedPullRequests.Clear();
        for (var i = 0; i < snapshot.OpenPullRequests.Count; i++)
        {
            OpenPullRequests.Add(new PullRequestRow(i + 1, snapshot.OpenPullRequests[i], snapshot.AsOf, _options));
        }

        for (var i = 0; i < snapshot.MergedPullRequests.Count; i++)
        {
            MergedPullRequests.Add(new PullRequestRow(i + 1, snapshot.MergedPullRequests[i], _options));
        }

        RepositoriesCount = snapshot.Repositories.Count;
        OpenPullRequestsCount = snapshot.OpenPullRequests.Count;
        MergedPullRequestsCount = snapshot.MergedPullRequests.Count;
        LoadTelemetry(snapshot.Telemetry);
    }

    private void IncreaseUiScale() => UiScale += UI_SCALE_STEP;

    private void DecreaseUiScale() => UiScale -= UI_SCALE_STEP;

    private void SaveLoadPreferences()
    {
        _preferences.SearchMode = SelectedSearchMode;
        _preferences.SearchPhrase = SearchPhrase;
        _preferencesService.Save(_preferences);
    }

    private static double NormalizeUiScale(double? value)
    {
        if (value is null || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
        {
            return DEFAULT_UI_SCALE;
        }

        return Math.Round(Math.Clamp(value.Value, MIN_UI_SCALE, MAX_UI_SCALE), 2);
    }

    private bool CanLoad() => !IsLoading && MergedPullRequestsDays > 0;

    private bool HasLoadedPullRequests() => OpenPullRequests.Count > 0 || MergedPullRequests.Count > 0;

    private static bool ConfirmReload()
    {
        var result = MessageBox.Show(
            "Pull requests are already loaded. Reload data from Bitbucket?",
            "Reload data",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        return result == MessageBoxResult.Yes;
    }

    private void Cancel() => _loadCancellation?.Cancel();

    private void OpenUrl(object? parameter)
    {
        if (parameter is not Uri url)
        {
            return;
        }
        _ = Process.Start(new ProcessStartInfo(url.ToString())
        {
            UseShellExecute = true
        });
    }

    private bool FilterOpenPullRequestRow(object item) => FilterPullRequestRow(item, OpenPullRequestFilters);

    private bool FilterMergedPullRequestRow(object item) => FilterPullRequestRow(item, MergedPullRequestFilters);

    private bool FilterPullRequestRow(object item, PullRequestFilterState filters)
    {
        if (item is not PullRequestRow row)
        {
            return false;
        }

        return Matches(row.SearchText, GlobalSearch)
            && Matches(row.Number.ToString(CultureInfo.InvariantCulture), filters.Number)
            && Matches(row.RepositoryName, filters.Repository)
            && Matches(row.PullRequestDisplay, filters.PullRequest)
            && Matches(row.Author, filters.Author)
            && Matches(row.DescriptionLength.ToString(CultureInfo.InvariantCulture), filters.DescriptionLength)
            && Matches(row.OpenFor, filters.OpenFor)
            && Matches(row.TimeToFirstResponse, filters.TimeToFirstResponse)
            && Matches(row.ActivityAgeOrMerged, filters.Activity)
            && Matches(row.CommentsCount.ToString(CultureInfo.InvariantCulture), filters.Comments)
            && Matches(row.RequestChanges, filters.RequestChanges)
            && Matches(row.Approvals, filters.Approvals)
            && Matches(row.CurrentUserActivity, filters.CurrentUserActivity);
    }

    private bool FilterTelemetryRow(object item) => item is TelemetryRow row && Matches(row.SearchText, TelemetryFilter);

    private void ShowLoadError(Exception exception)
    {
        Status = exception.Message;
        _ = MessageBox.Show(exception.Message, "Load failed", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void RefreshTelemetry()
    {
        LoadTelemetry(_telemetryService.GetSnapshot());
    }

    private void LoadTelemetry(BitbucketTelemetrySnapshot snapshot)
    {
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

    private void RefreshViews()
    {
        OpenPullRequestsView.Refresh();
        MergedPullRequestsView.Refresh();
    }

    private void SchedulePullRequestFilterRefresh()
    {
        _pullRequestFilterRefreshTimer.Stop();
        _pullRequestFilterRefreshTimer.Start();
    }

    private void ScheduleTelemetryFilterRefresh()
    {
        _telemetryFilterRefreshTimer.Stop();
        _telemetryFilterRefreshTimer.Start();
    }

    private void OnPullRequestFilterRefreshTimerTick(object? sender, EventArgs e)
    {
        _pullRequestFilterRefreshTimer.Stop();
        RefreshViews();
    }

    private void OnTelemetryFilterRefreshTimerTick(object? sender, EventArgs e)
    {
        _telemetryFilterRefreshTimer.Stop();
        TelemetryView.Refresh();
    }

    private void ResetFilters()
    {
        GlobalSearch = string.Empty;
        OpenPullRequestFilters.Reset();
        MergedPullRequestFilters.Reset();
        RaisePullRequestFilterPropertiesChanged();
        TelemetryFilter = string.Empty;
    }

    private void SetOpenPullRequestFilter(string value, Action<PullRequestFilterState, string> setFilter) => setFilter(OpenPullRequestFilters, value);

    private void SetMergedPullRequestFilter(string value, Action<PullRequestFilterState, string> setFilter) => setFilter(MergedPullRequestFilters, value);

    private void RaisePullRequestFilterPropertiesChanged()
    {
        OnPropertyChanged(nameof(OpenNumberFilter));
        OnPropertyChanged(nameof(OpenRepositoryFilter));
        OnPropertyChanged(nameof(OpenPullRequestFilter));
        OnPropertyChanged(nameof(OpenAuthorFilter));
        OnPropertyChanged(nameof(OpenDescriptionLengthFilter));
        OnPropertyChanged(nameof(OpenOpenForFilter));
        OnPropertyChanged(nameof(OpenTimeToFirstResponseFilter));
        OnPropertyChanged(nameof(OpenActivityFilter));
        OnPropertyChanged(nameof(OpenCommentsFilter));
        OnPropertyChanged(nameof(OpenRequestChangesFilter));
        OnPropertyChanged(nameof(OpenApprovalsFilter));
        OnPropertyChanged(nameof(OpenCurrentUserActivityFilter));
        OnPropertyChanged(nameof(MergedNumberFilter));
        OnPropertyChanged(nameof(MergedRepositoryFilter));
        OnPropertyChanged(nameof(MergedPullRequestFilter));
        OnPropertyChanged(nameof(MergedAuthorFilter));
        OnPropertyChanged(nameof(MergedDescriptionLengthFilter));
        OnPropertyChanged(nameof(MergedOpenForFilter));
        OnPropertyChanged(nameof(MergedTimeToFirstResponseFilter));
        OnPropertyChanged(nameof(MergedActivityFilter));
        OnPropertyChanged(nameof(MergedCommentsFilter));
        OnPropertyChanged(nameof(MergedRequestChangesFilter));
        OnPropertyChanged(nameof(MergedApprovalsFilter));
        OnPropertyChanged(nameof(MergedCurrentUserActivityFilter));
    }

    private static bool Matches(string source, string filter) => string.IsNullOrWhiteSpace(filter) || source.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);

    private void RaiseCommandStates()
    {
        ((RelayCommand)LoadCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
    }

    /// <summary>
    /// Releases resources held by the view model.
    /// </summary>
    public void Dispose()
    {
        _pullRequestFilterRefreshTimer.Stop();
        _telemetryFilterRefreshTimer.Stop();
        _loadCancellation?.Dispose();
    }

    private static readonly TimeSpan _filterRefreshDelay = TimeSpan.FromMilliseconds(150);

    private const double DEFAULT_UI_SCALE = 1.0;

    private const double MIN_UI_SCALE = 0.75;

    private const double MAX_UI_SCALE = 1.5;

    private const double UI_SCALE_STEP = 0.05;

    private readonly PullRequestDashboardLoader _loader;

    private readonly DemoPullRequestDashboardProvider _demoDataProvider;

    private readonly DemoTelemetryProvider _demoTelemetryProvider;

    private readonly BitbucketOptions _options;

    private readonly IBitbucketTelemetryService _telemetryService;

    private readonly UserPreferencesService _preferencesService;

    private readonly UserPreferences _preferences;

    private readonly DispatcherTimer _pullRequestFilterRefreshTimer;

    private readonly DispatcherTimer _telemetryFilterRefreshTimer;

    private int _mergedPullRequestsDays;

    private bool _isLightTheme;

    private double _uiScale;

    private CancellationTokenSource? _loadCancellation;

    private string _searchPhrase;

    private RepositorySearchMode _selectedSearchMode;
}
