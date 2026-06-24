using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
    /// <param name="loadCoordinator">Dashboard load coordinator.</param>
    /// <param name="telemetryDashboard">Telemetry dashboard view model.</param>
    /// <param name="rowMapper">Pull request row mapper.</param>
    /// <param name="dialogService">User-facing dialog service.</param>
    /// <param name="externalUrlLauncher">External URL launcher.</param>
    /// <param name="aiReviewPromptService">AI review prompt clipboard service.</param>
    /// <param name="preferencesService">User preferences persistence service.</param>
    public MainViewModel(
        IDashboardLoadCoordinator loadCoordinator,
        TelemetryViewModel telemetryDashboard,
        PullRequestRowMapper rowMapper,
        IDialogService dialogService,
        IExternalUrlLauncher externalUrlLauncher,
        IAiReviewPromptService aiReviewPromptService,
        IUserPreferencesService preferencesService)
    {
        ArgumentNullException.ThrowIfNull(loadCoordinator, nameof(loadCoordinator));
        ArgumentNullException.ThrowIfNull(telemetryDashboard, nameof(telemetryDashboard));
        ArgumentNullException.ThrowIfNull(rowMapper, nameof(rowMapper));
        ArgumentNullException.ThrowIfNull(dialogService, nameof(dialogService));
        ArgumentNullException.ThrowIfNull(externalUrlLauncher, nameof(externalUrlLauncher));
        ArgumentNullException.ThrowIfNull(aiReviewPromptService, nameof(aiReviewPromptService));
        ArgumentNullException.ThrowIfNull(preferencesService, nameof(preferencesService));
        _loadCoordinator = loadCoordinator;
        _rowMapper = rowMapper;
        _dialogService = dialogService;
        _externalUrlLauncher = externalUrlLauncher;
        _aiReviewPromptService = aiReviewPromptService;
        TelemetryDashboard = telemetryDashboard;
        _preferences = new MainViewModelPreferences(preferencesService);
        _isLightTheme = _preferences.IsLightTheme;
        _uiScale = _preferences.UiScale;
        _selectedSearchMode = _preferences.SearchMode;
        _searchPhrase = _preferences.SearchPhrase;
        _mergedPullRequestsDays = 1;
        _mergedPullRequestsDaysInput = _mergedPullRequestsDays.ToString(CultureInfo.InvariantCulture);
        _dashboardState = new PullRequestDashboardViewState(() => GlobalSearch);
        OpenPullRequestFilters = _dashboardState.OpenPullRequests.Filters;
        MergedPullRequestFilters = _dashboardState.MergedPullRequests.Filters;
        OpenPullRequestFilters.PropertyChanged += OnOpenPullRequestFilterPropertyChanged;
        MergedPullRequestFilters.PropertyChanged += OnMergedPullRequestFilterPropertyChanged;
        LoadCommand = new AsyncRelayCommand(LoadAsync, CanLoad);
        LoadCommand.PropertyChanged += OnLoadCommandPropertyChanged;
        LoadCommand.ExecutionFailed += OnLoadCommandExecutionFailed;
        CancelCommand = new RelayCommand(LoadCommand.Cancel, () => LoadCommand.CanBeCanceled);
        OpenUrlCommand = new RelayCommand(OpenUrl);
        CopyForAiCommand = new RelayCommand(
            CopyForAi,
            static parameter => parameter is PullRequestRow);
        ResetFiltersCommand = new RelayCommand(ResetFilters);
        ToggleOpenReviewedFilterCommand = new RelayCommand(ToggleOpenReviewedFilter);
        ToggleMergedReviewedFilterCommand = new RelayCommand(ToggleMergedReviewedFilter);
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
    public bool HasErrors => _validationErrors.HasErrors;

    /// <inheritdoc />
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged {
        add => _validationErrors.ErrorsChanged += value;
        remove => _validationErrors.ErrorsChanged -= value;
    }

    /// <inheritdoc />
    public System.Collections.IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return _validationErrors.GetErrors(propertyName);
        }

        return _validationErrors.GetErrors(propertyName);
    }

    /// <summary>
    /// Current application status text.
    /// </summary>
    public string Status {
        get;
        private set => SetProperty(ref field, value);
    } = "Ready";

    /// <summary>
    /// Local date and time when the currently displayed pull request data was loaded.
    /// </summary>
    public string LoadedAt {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the light UI theme is enabled.
    /// </summary>
    public bool IsLightTheme {
        get => _isLightTheme;
        set
        {
            if (SetProperty(ref _isLightTheme, value))
            {
                _preferences.SaveTheme(value);
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
            var normalizedValue = MainViewModelPreferences.NormalizeUiScale(value);
            if (SetProperty(ref _uiScale, normalizedValue))
            {
                _preferences.SaveUiScale(normalizedValue);
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
    /// Filter for the merged pull request row number column.
    /// </summary>
    public string MergedNumberFilter {
        get => MergedPullRequestFilters.Number;
        set => MergedPullRequestFilters.Number = value;
    }

    /// <summary>
    /// Filter for the merged pull request repository column.
    /// </summary>
    public string MergedRepositoryFilter {
        get => MergedPullRequestFilters.Repository;
        set => MergedPullRequestFilters.Repository = value;
    }

    /// <summary>
    /// Filter for the merged pull request column.
    /// </summary>
    public string MergedPullRequestFilter {
        get => MergedPullRequestFilters.PullRequest;
        set => MergedPullRequestFilters.PullRequest = value;
    }

    /// <summary>
    /// Filter for the merged pull request author column.
    /// </summary>
    public string MergedAuthorFilter {
        get => MergedPullRequestFilters.Author;
        set => MergedPullRequestFilters.Author = value;
    }

    /// <summary>
    /// Filter for the merged pull request description length column.
    /// </summary>
    public string MergedDescriptionLengthFilter {
        get => MergedPullRequestFilters.DescriptionLength;
        set => MergedPullRequestFilters.DescriptionLength = value;
    }

    /// <summary>
    /// Filter for the merged pull request TTFR column.
    /// </summary>
    public string MergedTimeToFirstResponseFilter {
        get => MergedPullRequestFilters.TimeToFirstResponse;
        set => MergedPullRequestFilters.TimeToFirstResponse = value;
    }

    /// <summary>
    /// Filter for the merged pull request merge age column.
    /// </summary>
    public string MergedActivityFilter {
        get => MergedPullRequestFilters.Activity;
        set => MergedPullRequestFilters.Activity = value;
    }

    /// <summary>
    /// Filter for the merged pull request comments column.
    /// </summary>
    public string MergedCommentsFilter {
        get => MergedPullRequestFilters.Comments;
        set => MergedPullRequestFilters.Comments = value;
    }

    /// <summary>
    /// Filter for the merged pull request changes column.
    /// </summary>
    public string MergedRequestChangesFilter {
        get => MergedPullRequestFilters.RequestChanges;
        set => MergedPullRequestFilters.RequestChanges = value;
    }

    /// <summary>
    /// Filter for the merged pull request approvals column.
    /// </summary>
    public string MergedApprovalsFilter {
        get => MergedPullRequestFilters.Approvals;
        set => MergedPullRequestFilters.Approvals = value;
    }

    /// <summary>
    /// Filter for the merged pull request current user activity column.
    /// </summary>
    public string MergedCurrentUserActivityFilter {
        get => MergedPullRequestFilters.CurrentUserActivity;
        set => MergedPullRequestFilters.CurrentUserActivity = value;
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
    public BulkObservableCollection<PullRequestRow> OpenPullRequestsView => _dashboardState.OpenPullRequests.View;

    /// <summary>
    /// Loaded recently merged pull request rows.
    /// </summary>
    public BulkObservableCollection<PullRequestRow> MergedPullRequestsView => _dashboardState.MergedPullRequests.View;

    /// <summary>
    /// Open pull request grid filters.
    /// </summary>
    public PullRequestFilterState OpenPullRequestFilters { get; }

    /// <summary>
    /// Recently merged pull request grid filters.
    /// </summary>
    public PullRequestFilterState MergedPullRequestFilters { get; }

    /// <summary>
    /// Text displayed by the open pull request reviewed filter button.
    /// </summary>
    public string OpenReviewedFilterButtonText =>
        OpenPullRequestFilters.HideReviewed ? "Show all" : "Hide reviewed";

    /// <summary>
    /// Text displayed by the merged pull request reviewed filter button.
    /// </summary>
    public string MergedReviewedFilterButtonText =>
        MergedPullRequestFilters.HideReviewed ? "Show all" : "Hide reviewed";

    /// <summary>
    /// Gets a value indicating whether reviewed open pull requests are hidden.
    /// </summary>
    public bool IsOpenReviewedFilterActive => OpenPullRequestFilters.HideReviewed;

    /// <summary>
    /// Gets a value indicating whether reviewed merged pull requests are hidden.
    /// </summary>
    public bool IsMergedReviewedFilterActive => MergedPullRequestFilters.HideReviewed;

    /// <summary>
    /// Telemetry dashboard view model.
    /// </summary>
    public TelemetryViewModel TelemetryDashboard { get; }

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
    /// Command that copies an AI review prompt for an open pull request.
    /// </summary>
    public ICommand CopyForAiCommand { get; }

    /// <summary>
    /// Command that clears all table filters.
    /// </summary>
    public ICommand ResetFiltersCommand { get; }

    /// <summary>
    /// Command that toggles hiding reviewed open pull requests.
    /// </summary>
    public ICommand ToggleOpenReviewedFilterCommand { get; }

    /// <summary>
    /// Command that toggles hiding reviewed merged pull requests.
    /// </summary>
    public ICommand ToggleMergedReviewedFilterCommand { get; }

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

        IsLoading = true;
        Status = "Starting";
        try
        {
            SaveLoadPreferences();

            var filterPattern = new FilterPattern(SearchPhrase, SelectedSearchMode);
            var isReload = HasLoadedPullRequests();
            var progress = new Progress<PullRequestLoadProgress>(value =>
            {
                Status = PullRequestLoadProgressFormatter.Format(value);
                _ = TelemetryDashboard.RefreshTelemetry();
            });
            var result = await _loadCoordinator
                .LoadAsync(
                    filterPattern,
                    MergedPullRequestsDays,
                    isReload,
                    _dashboardState.LoadedOpenPullRequests,
                    _dashboardState.LoadedMergedPullRequests,
                    progress,
                    cancellationToken)
                .ConfigureAwait(true);

            switch (result)
            {
                case DashboardLoadCoordinatorResult.Success success:
                    ApplyDashboardSnapshot(success.Snapshot);
                    Status = $"Loaded {OpenPullRequestsCount} open PRs and {MergedPullRequestsCount} merged PRs";
                    if (success.ReloadSummary is not null)
                    {
                        _dialogService.ShowReloadSummary(success.ReloadSummary);
                    }
                    break;
                case DashboardLoadCoordinatorResult.Cancelled:
                    Status = "Cancelled";
                    break;
                case DashboardLoadCoordinatorResult.Failure failure:
                    ShowLoadError(failure.UserMessage);
                    break;
                case DashboardLoadCoordinatorResult.Skipped:
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported dashboard load result: {result.GetType().Name}.");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyDashboardSnapshot(PullRequestDashboardSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _dashboardState.ApplySnapshot(snapshot, _rowMapper);

        RepositoriesCount = snapshot.Repositories.Count;
        OpenPullRequestsCount = snapshot.OpenPullRequests.Count;
        MergedPullRequestsCount = snapshot.MergedPullRequests.Count;
        LoadedAt = $"Loaded: {snapshot.AsOf.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)}";
        TelemetryDashboard.LoadTelemetry(snapshot.Telemetry);
    }

    private void IncreaseUiScale() => UiScale += UI_SCALE_STEP;

    private void DecreaseUiScale() => UiScale -= UI_SCALE_STEP;

    private void SaveLoadPreferences()
    {
        _preferences.SaveLoadPreferences(SelectedSearchMode, SearchPhrase);
    }

    private bool CanLoad() => !IsLoading && !HasErrors;

    private bool HasLoadedPullRequests() => _dashboardState.HasLoadedPullRequests;

    private void OpenUrl(object? parameter)
    {
        if (parameter is not Uri url)
        {
            return;
        }
        _externalUrlLauncher.Open(url);
    }

    private void CopyForAi(object? parameter)
    {
        if (parameter is not PullRequestRow pullRequest)
        {
            return;
        }

        _aiReviewPromptService.CopyPrompt(pullRequest);
        Status = $"Copied AI review prompt for {pullRequest.RepositoryName} #{pullRequest.PullRequestId}";
    }

    private void ShowLoadError(string message)
    {
        Status = message;
        _dialogService.ShowLoadError(message);
    }

    private void RefreshViews()
    {
        _dashboardState.Refresh();
    }

    private void SchedulePullRequestFilterRefresh() => RefreshViews();

    private void ToggleOpenReviewedFilter() => _dashboardState.OpenPullRequests.ToggleReviewedFilter();

    private void ToggleMergedReviewedFilter() => _dashboardState.MergedPullRequests.ToggleReviewedFilter();

    private void OnOpenPullRequestFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PullRequestFilterState.HideReviewed))
        {
            OnPropertyChanged(nameof(OpenReviewedFilterButtonText));
            OnPropertyChanged(nameof(IsOpenReviewedFilterActive));
        }
    }

    private void OnMergedPullRequestFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PullRequestFilterState.HideReviewed))
        {
            OnPropertyChanged(nameof(MergedReviewedFilterButtonText));
            OnPropertyChanged(nameof(IsMergedReviewedFilterActive));
        }
    }

    private void ResetFilters()
    {
        GlobalSearch = string.Empty;
        _dashboardState.ResetFilters();
        RaiseMergedFilterPropertiesChanged();
        TelemetryDashboard.ResetFilter();
    }

    private void RaiseMergedFilterPropertiesChanged()
    {
        OnPropertyChanged(nameof(MergedNumberFilter));
        OnPropertyChanged(nameof(MergedRepositoryFilter));
        OnPropertyChanged(nameof(MergedPullRequestFilter));
        OnPropertyChanged(nameof(MergedAuthorFilter));
        OnPropertyChanged(nameof(MergedDescriptionLengthFilter));
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
        var validation = MergedPullRequestPeriod.Validate(value);
        if (!validation.IsValid)
        {
            SetValidationError(propertyName, validation.Error ?? "Invalid merged pull request period.");
            return;
        }

        ClearValidationError(propertyName);
        MergedPullRequestsDays = validation.Days;
    }

    private void SetValidationError(string propertyName, string error)
    {
        _validationErrors.SetError(propertyName, error);
        OnPropertyChanged(nameof(HasErrors));
        RaiseCommandStates();
    }

    private void ClearValidationError(string propertyName)
    {
        if (!_validationErrors.ClearError(propertyName))
        {
            return;
        }

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

    private void OnLoadCommandExecutionFailed(object? sender, AsyncCommandFailedEventArgs e) =>
        ShowLoadError(e.Exception.Message);

    /// <summary>
    /// Releases resources held by the view model.
    /// </summary>
    public void Dispose()
    {
        _dashboardState.Dispose();
        OpenPullRequestFilters.PropertyChanged -= OnOpenPullRequestFilterPropertyChanged;
        MergedPullRequestFilters.PropertyChanged -= OnMergedPullRequestFilterPropertyChanged;
        LoadCommand.PropertyChanged -= OnLoadCommandPropertyChanged;
        LoadCommand.ExecutionFailed -= OnLoadCommandExecutionFailed;
        LoadCommand.Dispose();
    }

    private const double UI_SCALE_STEP = 0.05;

    private readonly IDashboardLoadCoordinator _loadCoordinator;

    private readonly PullRequestRowMapper _rowMapper;

    private readonly IDialogService _dialogService;

    private readonly IExternalUrlLauncher _externalUrlLauncher;

    private readonly IAiReviewPromptService _aiReviewPromptService;

    private readonly MainViewModelPreferences _preferences;

    private readonly PullRequestDashboardViewState _dashboardState;

    private readonly ValidationErrorStore _validationErrors = new();

    private int _mergedPullRequestsDays;

    private string _mergedPullRequestsDaysInput;

    private bool _isLightTheme;

    private double _uiScale;

    private string _searchPhrase;

    private RepositorySearchMode _selectedSearchMode;
}
