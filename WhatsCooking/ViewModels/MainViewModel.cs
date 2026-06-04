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
    /// <param name="options">Bitbucket configuration options.</param>
    /// <param name="preferencesService">User preferences persistence service.</param>
    public MainViewModel(PullRequestDashboardLoader loader, IBitbucketTelemetryService telemetryService, IOptions<BitbucketOptions> options, UserPreferencesService preferencesService)
    {
        ArgumentNullException.ThrowIfNull(loader, nameof(loader));
        ArgumentNullException.ThrowIfNull(telemetryService, nameof(telemetryService));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(preferencesService, nameof(preferencesService));
        _loader = loader;
        _telemetryService = telemetryService;
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
                LoadDemoData();
                return;
            }

            var filterPattern = new FilterPattern(SearchPhrase, SelectedSearchMode);
            var progress = new Progress<PullRequestLoadProgress>(value =>
            {
                Status = value.Message;
                RefreshTelemetry();
            });
            var result = await _loader.LoadAsync(filterPattern, MergedPullRequestsDays, progress, _loadCancellation.Token).ConfigureAwait(true);
            var asOf = DateTimeOffset.Now;
            OpenPullRequests.Clear();
            MergedPullRequests.Clear();
            for (var i = 0; i < result.OpenPullRequests.Count; i++)
            {
                OpenPullRequests.Add(new PullRequestRow(i + 1, result.OpenPullRequests[i], asOf, _options));
            }
            for (var i = 0; i < result.MergedPullRequests.Count; i++)
            {
                MergedPullRequests.Add(new PullRequestRow(i + 1, result.MergedPullRequests[i], _options));
            }
            RepositoriesCount = result.Repositories.Count;
            OpenPullRequestsCount = result.OpenPullRequests.Count;
            MergedPullRequestsCount = result.MergedPullRequests.Count;
            RefreshTelemetry();
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

    private void LoadDemoData()
    {
        Status = "Loading demo data";
        var asOf = DateTimeOffset.Now;
        var result = CreateDemoLoadResult(asOf);

        OpenPullRequests.Clear();
        MergedPullRequests.Clear();
        for (var i = 0; i < result.OpenPullRequests.Count; i++)
        {
            OpenPullRequests.Add(new PullRequestRow(i + 1, result.OpenPullRequests[i], asOf, _options));
        }

        for (var i = 0; i < result.MergedPullRequests.Count; i++)
        {
            MergedPullRequests.Add(new PullRequestRow(i + 1, result.MergedPullRequests[i], _options));
        }

        RepositoriesCount = result.Repositories.Count;
        OpenPullRequestsCount = result.OpenPullRequests.Count;
        MergedPullRequestsCount = result.MergedPullRequests.Count;
        LoadDemoTelemetry();
        Status = $"Loaded demo data: {OpenPullRequestsCount} open PRs and {MergedPullRequestsCount} merged PRs";
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

    private static PullRequestLoadResult CreateDemoLoadResult(DateTimeOffset asOf)
    {
        List<Repository> repositories =
        [
            CreateDemoRepository("Customer Portal", "customer-portal", 2),
            CreateDemoRepository("Billing Service", "billing-service", 1),
            CreateDemoRepository("Identity Gateway", "identity-gateway", 1),
            CreateDemoRepository("Reporting API", "reporting-api", 2),
            CreateDemoRepository("Mobile Backend", "mobile-backend", 1),
            CreateDemoRepository("Notification Worker", "notification-worker", 1),
            CreateDemoRepository("Admin Console", "admin-console", 2),
            CreateDemoRepository("Data Importer", "data-importer", 1),
            CreateDemoRepository("Search Indexer", "search-indexer", 1),
            CreateDemoRepository("Payments Adapter", "payments-adapter", 1)
        ];

        List<PullRequestDetail> openPullRequests =
        [
            CreateOpenDemoPullRequest(repositories[0], 1842, "Add saved filters to account activity grid", asOf.AddHours(-2.5), "Maya Ortiz", asOf.AddHours(-1.8), asOf.AddMinutes(-22), true, DEMO_DESCRIPTION_LONG, 7, 0, false, 2, false),
            CreateOpenDemoPullRequest(repositories[6], 517, "Refactor permission checks for tenant-scoped admin pages", asOf.AddHours(-7), "Ethan Brooks", asOf.AddHours(-5.5), asOf.AddMinutes(-50), false, DEMO_DESCRIPTION_LONG, 11, 1, true, 1, false),
            CreateOpenDemoPullRequest(repositories[1], 2291, "Fix retry metadata on invoice reconciliation job", asOf.AddHours(-11), "Priya Shah", asOf.AddHours(-9.5), asOf.AddHours(-2), true, DEMO_DESCRIPTION_LONG, 4, 0, false, 3, true),
            CreateOpenDemoPullRequest(repositories[3], 1438, "Expose export progress through report status endpoint", asOf.AddHours(-18), "Noah Stein", null, asOf.AddHours(-6), false, "Initial implementation.", 2, 0, false, 0, false),
            CreateOpenDemoPullRequest(repositories[4], 805, "Normalize device capability payload before caching", asOf.AddDays(-1.2), "Sofia Nguyen", asOf.AddDays(-1).AddHours(2), asOf.AddHours(-4), true, DEMO_DESCRIPTION_LONG, 9, 2, false, 1, false),
            CreateOpenDemoPullRequest(repositories[2], 662, "Add audit trail for external login callbacks", asOf.AddDays(-1.8), "Liam Chen", asOf.AddDays(-1.6), asOf.AddHours(-9), false, DEMO_DESCRIPTION_LONG, 6, 0, false, 2, false),
            CreateOpenDemoPullRequest(repositories[5], 331, "Throttle notification fan-out during incident windows", asOf.AddDays(-2.1), "Ava Martin", asOf.AddDays(-2).AddHours(4), asOf.AddHours(-13), true, DEMO_DESCRIPTION_LONG, 13, 1, false, 0, false),
            CreateOpenDemoPullRequest(repositories[8], 94, "Rebuild stale search documents after taxonomy changes", asOf.AddDays(-2.7), "Daniel Kim", asOf.AddDays(-2.4), asOf.AddDays(-1.1), false, DEMO_DESCRIPTION_LONG, 5, 0, false, 1, false),
            CreateOpenDemoPullRequest(repositories[9], 1206, "Support split authorization for partial refunds", asOf.AddDays(-3.2), "Nina Patel", asOf.AddDays(-3).AddHours(8), asOf.AddHours(-17), true, DEMO_DESCRIPTION_LONG, 15, 2, true, 2, false),
            CreateOpenDemoPullRequest(repositories[7], 408, "Validate imported column mappings before preview", asOf.AddDays(-4.1), "Oliver Reed", null, asOf.AddDays(-2.8), false, "Draft.", 1, 0, false, 0, false)
        ];

        List<MergedPullRequest> mergedPullRequests =
        [
            CreateMergedDemoPullRequest(repositories[3], 1432, "Cache generated report manifests for repeat downloads", asOf.AddDays(-2.8), "Maya Ortiz", asOf.AddDays(-2.6), asOf.AddHours(-3), true, asOf.AddHours(-2), DEMO_DESCRIPTION_LONG, 8, 0, false, 3, true),
            CreateMergedDemoPullRequest(repositories[0], 1838, "Preserve selected date range when switching accounts", asOf.AddDays(-3.1), "Ethan Brooks", asOf.AddDays(-3), asOf.AddHours(-5), false, asOf.AddHours(-4), DEMO_DESCRIPTION_LONG, 6, 0, false, 2, false),
            CreateMergedDemoPullRequest(repositories[6], 512, "Add keyboard navigation to role assignment dialog", asOf.AddDays(-4.5), "Sofia Nguyen", asOf.AddDays(-4.2), asOf.AddHours(-8), true, asOf.AddHours(-7), DEMO_DESCRIPTION_LONG, 10, 0, false, 2, true),
            CreateMergedDemoPullRequest(repositories[1], 2285, "Reduce lock contention in settlement batch writer", asOf.AddDays(-5.3), "Liam Chen", asOf.AddDays(-5), asOf.AddHours(-12), false, asOf.AddHours(-11), DEMO_DESCRIPTION_LONG, 12, 1, false, 3, false),
            CreateMergedDemoPullRequest(repositories[5], 326, "Move email template rendering behind queue boundary", asOf.AddDays(-5.9), "Noah Stein", asOf.AddDays(-5.7), asOf.AddHours(-16), true, asOf.AddHours(-15), DEMO_DESCRIPTION_LONG, 4, 0, false, 1, false),
            CreateMergedDemoPullRequest(repositories[4], 799, "Add telemetry for mobile session refresh failures", asOf.AddDays(-6.4), "Priya Shah", asOf.AddDays(-6.2), asOf.AddDays(-1), false, asOf.AddHours(-19), DEMO_DESCRIPTION_LONG, 9, 0, false, 2, false),
            CreateMergedDemoPullRequest(repositories[2], 657, "Handle expired SSO assertions with clear login state", asOf.AddDays(-7.1), "Nina Patel", asOf.AddDays(-6.9), asOf.AddDays(-1.4), true, asOf.AddDays(-1.1), DEMO_DESCRIPTION_LONG, 7, 0, false, 3, true),
            CreateMergedDemoPullRequest(repositories[8], 89, "Compact index rebuild batches by content type", asOf.AddDays(-8.2), "Daniel Kim", asOf.AddDays(-8), asOf.AddDays(-1.8), false, asOf.AddDays(-1.5), DEMO_DESCRIPTION_LONG, 5, 0, false, 1, false),
            CreateMergedDemoPullRequest(repositories[9], 1198, "Record gateway reference IDs for dispute lookup", asOf.AddDays(-9.3), "Ava Martin", asOf.AddDays(-9), asOf.AddDays(-2.2), true, asOf.AddDays(-2), DEMO_DESCRIPTION_LONG, 11, 0, false, 2, false),
            CreateMergedDemoPullRequest(repositories[7], 402, "Improve CSV preview error grouping", asOf.AddDays(-10.5), "Oliver Reed", asOf.AddDays(-10.2), asOf.AddDays(-3), false, asOf.AddDays(-2.6), DEMO_DESCRIPTION_LONG, 3, 0, false, 1, false)
        ];

        return new PullRequestLoadResult(repositories, openPullRequests, mergedPullRequests);
    }

    private void LoadDemoTelemetry()
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

        IsTelemetryEnabled = true;
        TelemetryRequestsCount = totalRequests;
        TelemetryEndpointsCount = statistics.Count;
        Telemetry.Clear();
        for (var i = 0; i < statistics.Count; i++)
        {
            Telemetry.Add(new TelemetryRow(i + 1, statistics[i], totalRequests));
        }

        TelemetryView.Refresh();
    }

    private static Repository CreateDemoRepository(string name, string slug, int openPullRequestsCount)
    {
        var repository = new Repository(
            name,
            DateTimeOffset.Now.AddYears(-2),
            DateTimeOffset.Now.AddDays(-openPullRequestsCount),
            new RepositorySlug(slug));
        repository.UpdateOpenPullRequestsCount(openPullRequestsCount);
        return repository;
    }

    private static PullRequestDetail CreateOpenDemoPullRequest(
        Repository repository,
        int pullRequestId,
        string title,
        DateTimeOffset openedOn,
        string author,
        DateTimeOffset? firstResponseOn,
        DateTimeOffset? lastActivityOn,
        bool hasCurrentUserDiscussion,
        string description,
        int commentsCount,
        int requestChangesCount,
        bool hasCurrentUserRequestChanges,
        int approvalsCount,
        bool hasCurrentUserApproval) =>
        new(
            repository,
            new PullRequestId(pullRequestId),
            title,
            openedOn,
            new BitbucketId($"demo-user-{NormalizeDemoId(author)}"),
            author,
            firstResponseOn,
            lastActivityOn,
            hasCurrentUserDiscussion,
            description,
            commentsCount,
            requestChangesCount,
            hasCurrentUserRequestChanges,
            approvalsCount,
            hasCurrentUserApproval);

    private static MergedPullRequest CreateMergedDemoPullRequest(
        Repository repository,
        int pullRequestId,
        string title,
        DateTimeOffset openedOn,
        string author,
        DateTimeOffset? firstResponseOn,
        DateTimeOffset? lastActivityOn,
        bool hasCurrentUserDiscussion,
        DateTimeOffset mergedOn,
        string description,
        int commentsCount,
        int requestChangesCount,
        bool hasCurrentUserRequestChanges,
        int approvalsCount,
        bool hasCurrentUserApproval) =>
        new(
            repository,
            new PullRequestId(pullRequestId),
            title,
            openedOn,
            new BitbucketId($"demo-user-{NormalizeDemoId(author)}"),
            author,
            firstResponseOn,
            lastActivityOn,
            hasCurrentUserDiscussion,
            mergedOn,
            description,
            commentsCount,
            requestChangesCount,
            hasCurrentUserRequestChanges,
            approvalsCount,
            hasCurrentUserApproval);

    private static string NormalizeDemoId(string value) => value.Replace(' ', '-').ToUpperInvariant();

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
        var snapshot = _telemetryService.GetSnapshot();
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

    private const string DEMO_DESCRIPTION_LONG = "Synthetic demo pull request with realistic review data for presenting the dashboard without Bitbucket credentials.";

    private readonly PullRequestDashboardLoader _loader;

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
