using System.Globalization;

using BBRepoList.Models;

using WhatsCooking.Services;

namespace WhatsCooking.ViewModels;

internal sealed partial class MainViewModel
{
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

    private void SaveLoadPreferences()
    {
        _preferences.SaveLoadPreferences(SelectedSearchMode, SearchPhrase);
    }

    private bool CanLoad() => !IsLoading && !HasErrors;

    private bool HasLoadedPullRequests() => _dashboardState.HasLoadedPullRequests;

    private void ShowLoadError(string message)
    {
        Status = message;
        _dialogService.ShowLoadError(message);
    }
}
