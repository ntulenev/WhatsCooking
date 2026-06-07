using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Windows.Input;

using BBRepoList.Models;

using WhatsCooking.Services;

namespace WhatsCooking.ViewModels;

/// <summary>
/// View model for the pull request dashboard window.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "View model is created by dependency injection.")]
internal sealed class MainViewModel : ObservableObject, INotifyDataErrorInfo, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="loadUseCase">Dashboard loading use case.</param>
    /// <param name="telemetryDashboard">Telemetry dashboard view model.</param>
    /// <param name="rowFactory">Pull request row factory.</param>
    /// <param name="dialogService">User-facing dialog service.</param>
    /// <param name="externalUrlLauncher">External URL launcher.</param>
    /// <param name="filterDebouncer">Debouncer used for table filters.</param>
    /// <param name="preferencesService">User preferences persistence service.</param>
    public MainViewModel(
        IDashboardLoadUseCase loadUseCase,
        ITelemetryDashboard telemetryDashboard,
        IPullRequestRowFactory rowFactory,
        IDialogService dialogService,
        IExternalUrlLauncher externalUrlLauncher,
        IDebouncer filterDebouncer,
        IUserPreferencesService preferencesService)
    {
        ArgumentNullException.ThrowIfNull(loadUseCase, nameof(loadUseCase));
        ArgumentNullException.ThrowIfNull(telemetryDashboard, nameof(telemetryDashboard));
        ArgumentNullException.ThrowIfNull(rowFactory, nameof(rowFactory));
        ArgumentNullException.ThrowIfNull(dialogService, nameof(dialogService));
        ArgumentNullException.ThrowIfNull(externalUrlLauncher, nameof(externalUrlLauncher));
        ArgumentNullException.ThrowIfNull(filterDebouncer, nameof(filterDebouncer));
        ArgumentNullException.ThrowIfNull(preferencesService, nameof(preferencesService));
        _loadUseCase = loadUseCase;
        _rowFactory = rowFactory;
        _dialogService = dialogService;
        _externalUrlLauncher = externalUrlLauncher;
        _filterDebouncer = filterDebouncer;
        TelemetryDashboard = telemetryDashboard;
        _preferencesService = preferencesService;
        _preferences = _preferencesService.Load();
        _isLightTheme = _preferences.IsLightTheme;
        _uiScale = NormalizeUiScale(_preferences.UiScale);
        _selectedSearchMode = _preferences.SearchMode ?? RepositorySearchMode.StartWith;
        _searchPhrase = _preferences.SearchPhrase ?? string.Empty;
        _mergedPullRequestsDays = 1;
        _mergedPullRequestsDaysInput = _mergedPullRequestsDays.ToString(CultureInfo.InvariantCulture);
        OpenPullRequestFilters = new PullRequestFilterState(SchedulePullRequestFilterRefresh);
        MergedPullRequestFilters = new PullRequestFilterState(SchedulePullRequestFilterRefresh);
        LoadCommand = new AsyncRelayCommand(LoadAsync, CanLoad);
        LoadCommand.PropertyChanged += OnLoadCommandPropertyChanged;
        CancelCommand = new RelayCommand(LoadCommand.Cancel, () => LoadCommand.CanBeCanceled);
        OpenUrlCommand = new RelayCommand(OpenUrl);
        ResetFiltersCommand = new RelayCommand(ResetFilters);
        IncreaseUiScaleCommand = new RelayCommand(IncreaseUiScale);
        DecreaseUiScaleCommand = new RelayCommand(DecreaseUiScale);
        _ = TelemetryDashboard.RefreshTelemetry();
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
        private set
        {
            if (SetProperty(ref _mergedPullRequestsDays, value))
            {
                RaiseCommandStates();
            }
        }
    }

    /// <summary>
    /// Editable number of days used to load recently merged pull requests.
    /// </summary>
    public string MergedPullRequestsDaysInput {
        get => _mergedPullRequestsDaysInput;
        set
        {
            var normalizedValue = value ?? string.Empty;
            if (SetProperty(ref _mergedPullRequestsDaysInput, normalizedValue))
            {
                ValidateMergedPullRequestsDays(normalizedValue);
            }
        }
    }

    /// <inheritdoc />
    public bool HasErrors => _validationErrors.Count > 0;

    /// <inheritdoc />
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <inheritdoc />
    public System.Collections.IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return _validationErrors.Values.SelectMany(static errors => errors);
        }

        return _validationErrors.TryGetValue(propertyName, out var errors)
            ? errors
            : [];
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
                _preferences = _preferences with { IsLightTheme = value };
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
                _preferences = _preferences with { UiScale = normalizedValue };
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
    /// Loaded open pull request rows.
    /// </summary>
    public BulkObservableCollection<PullRequestRow> OpenPullRequestsView { get; } = [];

    /// <summary>
    /// Loaded recently merged pull request rows.
    /// </summary>
    public BulkObservableCollection<PullRequestRow> MergedPullRequestsView { get; } = [];

    /// <summary>
    /// Open pull request grid filters.
    /// </summary>
    public PullRequestFilterState OpenPullRequestFilters { get; }

    /// <summary>
    /// Recently merged pull request grid filters.
    /// </summary>
    public PullRequestFilterState MergedPullRequestFilters { get; }

    /// <summary>
    /// Telemetry dashboard view model.
    /// </summary>
    public ITelemetryDashboard TelemetryDashboard { get; }

    /// <summary>
    /// Command that loads pull request data from Bitbucket.
    /// </summary>
    public AsyncRelayCommand LoadCommand { get; }

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

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (!CanLoad())
        {
            return;
        }

        if (HasLoadedPullRequests() && !_dialogService.ConfirmReload())
        {
            return;
        }

        IsLoading = true;
        Status = "Starting";
        try
        {
            SaveLoadPreferences();

            var filterPattern = new FilterPattern(SearchPhrase, SelectedSearchMode);
            var progress = new Progress<PullRequestLoadProgress>(value =>
            {
                Status = value.Message;
                _ = TelemetryDashboard.RefreshTelemetry();
            });
            var snapshot = await _loadUseCase
                .LoadAsync(filterPattern, MergedPullRequestsDays, progress, cancellationToken)
                .ConfigureAwait(true);
            ApplyDashboardSnapshot(snapshot);
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
            IsLoading = false;
        }
    }

    private void ApplyDashboardSnapshot(PullRequestDashboardSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _openPullRequests.Clear();
        _openPullRequests.AddRange(snapshot.OpenPullRequests.Select(
            (pullRequest, index) => _rowFactory.CreateOpenRow(index + 1, pullRequest, snapshot.AsOf)));
        _mergedPullRequests.Clear();
        _mergedPullRequests.AddRange(snapshot.MergedPullRequests.Select(
            (pullRequest, index) => _rowFactory.CreateMergedRow(index + 1, pullRequest, snapshot.AsOf)));
        RefreshViews();

        RepositoriesCount = snapshot.Repositories.Count;
        OpenPullRequestsCount = snapshot.OpenPullRequests.Count;
        MergedPullRequestsCount = snapshot.MergedPullRequests.Count;
        TelemetryDashboard.LoadTelemetry(snapshot.Telemetry);
    }

    private void IncreaseUiScale() => UiScale += UI_SCALE_STEP;

    private void DecreaseUiScale() => UiScale -= UI_SCALE_STEP;

    private void SaveLoadPreferences()
    {
        _preferences = _preferences with
        {
            SearchMode = SelectedSearchMode,
            SearchPhrase = SearchPhrase
        };
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

    private bool CanLoad() => !IsLoading && !HasErrors;

    private bool HasLoadedPullRequests() => _openPullRequests.Count > 0 || _mergedPullRequests.Count > 0;

    private void OpenUrl(object? parameter)
    {
        if (parameter is not Uri url)
        {
            return;
        }
        _externalUrlLauncher.Open(url);
    }

    private void ShowLoadError(Exception exception)
    {
        Status = exception.Message;
        _dialogService.ShowLoadError(exception.Message);
    }

    private void RefreshViews()
    {
        OpenPullRequestsView.ReplaceAll(
            _openPullRequests.Where(row => PullRequestRowFilter.Matches(row, GlobalSearch, OpenPullRequestFilters)));
        MergedPullRequestsView.ReplaceAll(
            _mergedPullRequests.Where(row => PullRequestRowFilter.Matches(row, GlobalSearch, MergedPullRequestFilters)));
    }

    private void SchedulePullRequestFilterRefresh() =>
        _filterDebouncer.Schedule(RefreshViews, _filterRefreshDelay);

    private void ResetFilters()
    {
        GlobalSearch = string.Empty;
        OpenPullRequestFilters.Reset();
        MergedPullRequestFilters.Reset();
        RaisePullRequestFilterPropertiesChanged();
        TelemetryDashboard.ResetFilter();
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

    private void RaiseCommandStates()
    {
        LoadCommand.RaiseCanExecuteChanged();
        ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
    }

    private void ValidateMergedPullRequestsDays(string value)
    {
        const string propertyName = nameof(MergedPullRequestsDaysInput);
        if (!MergedPullRequestPeriod.TryParse(value, out var days))
        {
            SetValidationError(
                propertyName,
                $"Enter a whole number from {MergedPullRequestPeriod.MINIMUM_DAYS} to {MergedPullRequestPeriod.MAXIMUM_DAYS}.");
            return;
        }

        ClearValidationError(propertyName);
        MergedPullRequestsDays = days;
    }

    private void SetValidationError(string propertyName, string error)
    {
        _validationErrors[propertyName] = [error];
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
        RaiseCommandStates();
    }

    private void ClearValidationError(string propertyName)
    {
        if (!_validationErrors.Remove(propertyName))
        {
            return;
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
        RaiseCommandStates();
    }

    private void OnLoadCommandPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AsyncRelayCommand.IsRunning) or nameof(AsyncRelayCommand.CanBeCanceled))
        {
            ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Releases resources held by the view model.
    /// </summary>
    public void Dispose()
    {
        LoadCommand.PropertyChanged -= OnLoadCommandPropertyChanged;
        LoadCommand.Dispose();
        _filterDebouncer.Dispose();
    }

    private static readonly TimeSpan _filterRefreshDelay = TimeSpan.FromMilliseconds(150);

    private const double DEFAULT_UI_SCALE = 1.0;

    private const double MIN_UI_SCALE = 0.75;

    private const double MAX_UI_SCALE = 1.5;

    private const double UI_SCALE_STEP = 0.05;

    private readonly IDashboardLoadUseCase _loadUseCase;

    private readonly IPullRequestRowFactory _rowFactory;

    private readonly IDialogService _dialogService;

    private readonly IExternalUrlLauncher _externalUrlLauncher;

    private readonly IDebouncer _filterDebouncer;

    private readonly IUserPreferencesService _preferencesService;

    private UserPreferences _preferences;

    private readonly List<PullRequestRow> _openPullRequests = [];

    private readonly List<PullRequestRow> _mergedPullRequests = [];

    private readonly Dictionary<string, string[]> _validationErrors = new(StringComparer.Ordinal);

    private int _mergedPullRequestsDays;

    private string _mergedPullRequestsDaysInput;

    private bool _isLightTheme;

    private double _uiScale;

    private string _searchPhrase;

    private RepositorySearchMode _selectedSearchMode;
}
