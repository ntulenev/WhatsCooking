using System.Globalization;

using BBRepoList.Models;

using WhatsCooking.Services;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Executes dashboard loads and applies successful snapshots to dashboard state.
/// </summary>
internal sealed class DashboardLoadCommandHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardLoadCommandHandler"/> class.
    /// </summary>
    public DashboardLoadCommandHandler(
        IDashboardLoadCoordinator loadCoordinator,
        IPullRequestRowMapper rowMapper,
        IDialogService dialogService,
        MainViewModelPreferences preferences,
        PullRequestDashboardViewState dashboardState,
        ITelemetryDashboard telemetryDashboard)
    {
        ArgumentNullException.ThrowIfNull(loadCoordinator);
        ArgumentNullException.ThrowIfNull(rowMapper);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(dashboardState);
        ArgumentNullException.ThrowIfNull(telemetryDashboard);

        _loadCoordinator = loadCoordinator;
        _rowMapper = rowMapper;
        _dialogService = dialogService;
        _preferences = preferences;
        _dashboardState = dashboardState;
        _telemetryDashboard = telemetryDashboard;
    }

    /// <summary>
    /// Loads dashboard data and returns bindable summary values.
    /// </summary>
    public async Task<DashboardLoadCommandResult> LoadAsync(
        RepositorySearchMode searchMode,
        string searchPhrase,
        int mergedPullRequestsDays,
        IProgress<string> statusProgress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(statusProgress);

        _preferences.SaveLoadPreferences(searchMode, searchPhrase);

        var filterPattern = new FilterPattern(searchPhrase, searchMode);
        var isReload = _dashboardState.HasLoadedPullRequests;
        var progress = new Progress<PullRequestLoadProgress>(value =>
        {
            statusProgress.Report(PullRequestLoadProgressFormatter.Format(value));
            _ = _telemetryDashboard.RefreshTelemetry();
        });
        var result = await _loadCoordinator
            .LoadAsync(
                filterPattern,
                mergedPullRequestsDays,
                isReload,
                _dashboardState.LoadedOpenPullRequests,
                _dashboardState.LoadedMergedPullRequests,
                progress,
                cancellationToken)
            .ConfigureAwait(true);

        return result switch
        {
            DashboardLoadCoordinatorResult.Success success => ApplySuccess(success),
            DashboardLoadCoordinatorResult.Cancelled => DashboardLoadCommandResult.Cancelled,
            DashboardLoadCoordinatorResult.Failure failure => ReportFailure(failure.UserMessage),
            DashboardLoadCoordinatorResult.Skipped => DashboardLoadCommandResult.Skipped,
            _ => throw new InvalidOperationException($"Unsupported dashboard load result: {result.GetType().Name}.")
        };
    }

    private DashboardLoadCommandResult ApplySuccess(DashboardLoadCoordinatorResult.Success success)
    {
        var snapshot = success.Snapshot;
        _dashboardState.ApplySnapshot(snapshot, _rowMapper);
        _telemetryDashboard.LoadTelemetry(snapshot.Telemetry);

        if (success.ReloadSummary is not null)
        {
            _dialogService.ShowReloadSummary(success.ReloadSummary);
        }

        return new DashboardLoadCommandResult(
            $"Loaded {snapshot.OpenPullRequests.Count} open PRs and {snapshot.MergedPullRequests.Count} merged PRs",
            snapshot.Repositories.Count,
            snapshot.OpenPullRequests.Count,
            snapshot.MergedPullRequests.Count,
            $"Loaded: {snapshot.AsOf.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)}");
    }

    /// <summary>
    /// Shows a load failure and returns the bindable failure state.
    /// </summary>
    public DashboardLoadCommandResult ReportFailure(string message)
    {
        _dialogService.ShowLoadError(message);
        return DashboardLoadCommandResult.Failure(message);
    }

    private readonly IDashboardLoadCoordinator _loadCoordinator;

    private readonly IPullRequestRowMapper _rowMapper;

    private readonly IDialogService _dialogService;

    private readonly MainViewModelPreferences _preferences;

    private readonly PullRequestDashboardViewState _dashboardState;

    private readonly ITelemetryDashboard _telemetryDashboard;
}
