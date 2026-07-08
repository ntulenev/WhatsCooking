using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Input;

using BBRepoList.Models;

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
    /// <param name="telemetryDashboard">Telemetry dashboard state.</param>
    /// <param name="dashboardContextFactory">Main dashboard context factory.</param>
    /// <param name="userActions">User-triggered dashboard actions.</param>
    public MainViewModel(
        ITelemetryDashboard telemetryDashboard,
        IMainDashboardContextFactory dashboardContextFactory,
        IDashboardUserActions userActions)
    {
        ArgumentNullException.ThrowIfNull(telemetryDashboard, nameof(telemetryDashboard));
        ArgumentNullException.ThrowIfNull(dashboardContextFactory, nameof(dashboardContextFactory));
        ArgumentNullException.ThrowIfNull(userActions, nameof(userActions));
        _userActions = userActions;
        TelemetryDashboard = telemetryDashboard;
        var dashboardContext = dashboardContextFactory.Create(() => GlobalSearch, telemetryDashboard);
        _preferences = dashboardContext.Preferences;
        _themeMode = _preferences.ThemeMode;
        _uiScale = _preferences.UiScale;
        _selectedSearchMode = _preferences.SearchMode;
        _searchPhrase = _preferences.SearchPhrase;
        _mergedPullRequestsDays = 1;
        _mergedPullRequestsDaysInput = _mergedPullRequestsDays.ToString(CultureInfo.InvariantCulture);
        _dashboardState = dashboardContext.DashboardState;
        _dashboardLoader = dashboardContext.DashboardLoader;
        _themeOptions = AppThemeOptions.All;
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
        SelectNextThemeCommand = new RelayCommand(SelectNextTheme);
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
        get => ThemeMode == AppThemeMode.Light;
        set
        {
            ThemeMode = value ? AppThemeMode.Light : AppThemeMode.Dark;
        }
    }

    /// <summary>
    /// Gets or sets the selected UI theme.
    /// </summary>
    public AppThemeMode ThemeMode {
        get => _themeMode;
        set
        {
            if (SetProperty(ref _themeMode, value))
            {
                _preferences.SaveTheme(value);
                OnPropertyChanged(nameof(IsLightTheme));
            }
        }
    }

    /// <summary>
    /// Theme options available in the UI.
    /// </summary>
    public IReadOnlyList<AppThemeOption> ThemeOptions => _themeOptions;

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

    /// <summary>
    /// Command that selects the next UI theme.
    /// </summary>
    public ICommand SelectNextThemeCommand { get; }

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
            var result = await _dashboardLoader
                .LoadAsync(
                    SelectedSearchMode,
                    SearchPhrase,
                    MergedPullRequestsDays,
                    new Progress<string>(value => Status = value),
                    cancellationToken)
                .ConfigureAwait(true);

            ApplyLoadResult(result);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void IncreaseUiScale() => UiScale += UI_SCALE_STEP;

    private void DecreaseUiScale() => UiScale -= UI_SCALE_STEP;

    private void SelectNextTheme()
    {
        if (_themeOptions.Count == 0)
        {
            return;
        }

        var currentIndex = -1;
        for (var index = 0; index < _themeOptions.Count; index++)
        {
            if (_themeOptions[index].Mode == ThemeMode)
            {
                currentIndex = index;
                break;
            }
        }

        ThemeMode = _themeOptions[(currentIndex + 1) % _themeOptions.Count].Mode;
    }

    private bool CanLoad() => !IsLoading && !HasErrors;

    private void OpenUrl(object? parameter)
    {
        if (parameter is not Uri url)
        {
            return;
        }
        _userActions.OpenUrl(url);
    }

    private void CopyForAi(object? parameter)
    {
        if (parameter is not PullRequestRow pullRequest)
        {
            return;
        }

        Status = _userActions.CopyAiReviewPrompt(pullRequest);
    }

    private void ApplyLoadResult(DashboardLoadCommandResult result)
    {
        if (!string.IsNullOrEmpty(result.Status))
        {
            Status = result.Status;
        }

        if (result.RepositoriesCount.HasValue)
        {
            RepositoriesCount = result.RepositoriesCount.Value;
        }

        if (result.OpenPullRequestsCount.HasValue)
        {
            OpenPullRequestsCount = result.OpenPullRequestsCount.Value;
        }

        if (result.MergedPullRequestsCount.HasValue)
        {
            MergedPullRequestsCount = result.MergedPullRequestsCount.Value;
        }

        if (result.LoadedAt is not null)
        {
            LoadedAt = result.LoadedAt;
        }
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
        TelemetryDashboard.ResetFilter();
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
        ApplyLoadResult(_dashboardLoader.ReportFailure(e.Exception.Message));

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

    private readonly IDashboardUserActions _userActions;

    private readonly MainViewModelPreferences _preferences;

    private readonly PullRequestDashboardViewState _dashboardState;

    private readonly DashboardLoadCommandHandler _dashboardLoader;

    private readonly IReadOnlyList<AppThemeOption> _themeOptions;

    private readonly ValidationErrorStore _validationErrors = new();

    private int _mergedPullRequestsDays;

    private string _mergedPullRequestsDaysInput;

    private AppThemeMode _themeMode;

    private double _uiScale;

    private string _searchPhrase;

    private RepositorySearchMode _selectedSearchMode;
}
