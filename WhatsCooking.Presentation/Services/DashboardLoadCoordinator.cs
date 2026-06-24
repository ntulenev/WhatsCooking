using BBRepoList.Models;

namespace WhatsCooking.Services;

/// <summary>
/// Coordinates dashboard load execution and reload-specific decisions.
/// </summary>
internal sealed class DashboardLoadCoordinator : IDashboardLoadCoordinator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardLoadCoordinator"/> class.
    /// </summary>
    /// <param name="loadUseCase">Dashboard loading use case.</param>
    /// <param name="reloadSummaryService">Reload summary service.</param>
    /// <param name="dialogService">User-facing dialog service.</param>
    public DashboardLoadCoordinator(
        IDashboardLoadUseCase loadUseCase,
        IDashboardReloadSummaryService reloadSummaryService,
        IDialogService dialogService)
    {
        ArgumentNullException.ThrowIfNull(loadUseCase);
        ArgumentNullException.ThrowIfNull(reloadSummaryService);
        ArgumentNullException.ThrowIfNull(dialogService);

        _loadUseCase = loadUseCase;
        _reloadSummaryService = reloadSummaryService;
        _dialogService = dialogService;
    }

    /// <inheritdoc />
    public async Task<DashboardLoadCoordinatorResult> LoadAsync(
        FilterPattern filterPattern,
        int mergedPullRequestsDays,
        bool isReload,
        IReadOnlyCollection<PullRequestDetail> previousOpenPullRequests,
        IReadOnlyCollection<MergedPullRequest> previousMergedPullRequests,
        IProgress<PullRequestLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(previousOpenPullRequests);
        ArgumentNullException.ThrowIfNull(previousMergedPullRequests);

        if (isReload && !_dialogService.ConfirmReload())
        {
            return new DashboardLoadCoordinatorResult.Skipped();
        }

        var result = await _loadUseCase
            .LoadAsync(filterPattern, mergedPullRequestsDays, progress, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            DashboardLoadResult.Success success => new DashboardLoadCoordinatorResult.Success(
                success.Snapshot,
                isReload
                    ? _reloadSummaryService.CreateSummary(
                        previousOpenPullRequests,
                        previousMergedPullRequests,
                        success.Snapshot)
                    : null),
            DashboardLoadResult.Cancelled => new DashboardLoadCoordinatorResult.Cancelled(),
            DashboardLoadResult.Failure failure => new DashboardLoadCoordinatorResult.Failure(failure.UserMessage),
            _ => throw new InvalidOperationException($"Unsupported dashboard load result: {result.GetType().Name}.")
        };
    }

    private readonly IDashboardLoadUseCase _loadUseCase;

    private readonly IDashboardReloadSummaryService _reloadSummaryService;

    private readonly IDialogService _dialogService;
}
