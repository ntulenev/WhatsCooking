using BBRepoList.Models;

using WhatsCooking.Services;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Maintains pull request dashboard grid state and the latest loaded domain items.
/// </summary>
internal sealed class PullRequestDashboardViewState : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestDashboardViewState"/> class.
    /// </summary>
    /// <param name="getGlobalSearch">Returns the current global search text.</param>
    public PullRequestDashboardViewState(Func<string> getGlobalSearch)
    {
        ArgumentNullException.ThrowIfNull(getGlobalSearch);

        OpenPullRequests = new PullRequestGridViewState(getGlobalSearch);
        MergedPullRequests = new PullRequestGridViewState(getGlobalSearch);
    }

    /// <summary>
    /// Open pull request grid state.
    /// </summary>
    public PullRequestGridViewState OpenPullRequests { get; }

    /// <summary>
    /// Recently merged pull request grid state.
    /// </summary>
    public PullRequestGridViewState MergedPullRequests { get; }

    /// <summary>
    /// Open pull requests from the latest loaded snapshot.
    /// </summary>
    public IReadOnlyCollection<PullRequestDetail> LoadedOpenPullRequests { get; private set; } = [];

    /// <summary>
    /// Merged pull requests from the latest loaded snapshot.
    /// </summary>
    public IReadOnlyCollection<MergedPullRequest> LoadedMergedPullRequests { get; private set; } = [];

    /// <summary>
    /// Gets a value indicating whether any pull requests have been loaded.
    /// </summary>
    public bool HasLoadedPullRequests => OpenPullRequests.Count > 0 || MergedPullRequests.Count > 0;

    /// <summary>
    /// Applies a loaded dashboard snapshot to both pull request grids.
    /// </summary>
    /// <param name="snapshot">Loaded dashboard data.</param>
    /// <param name="rowMapper">Maps domain pull requests to grid rows.</param>
    public void ApplySnapshot(PullRequestDashboardSnapshot snapshot, IPullRequestRowMapper rowMapper)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(rowMapper);

        OpenPullRequests.ReplaceAll(snapshot.OpenPullRequests.Select(
            (pullRequest, index) => rowMapper.MapOpen(index + 1, pullRequest, snapshot.AsOf)));
        MergedPullRequests.ReplaceAll(snapshot.MergedPullRequests.Select(
            (pullRequest, index) => rowMapper.MapMerged(index + 1, pullRequest, snapshot.AsOf)));
        LoadedOpenPullRequests = [.. snapshot.OpenPullRequests];
        LoadedMergedPullRequests = [.. snapshot.MergedPullRequests];
    }

    /// <summary>
    /// Refreshes both filtered grid views.
    /// </summary>
    public void Refresh()
    {
        OpenPullRequests.Refresh();
        MergedPullRequests.Refresh();
    }

    /// <summary>
    /// Clears filters for both pull request grids.
    /// </summary>
    public void ResetFilters()
    {
        OpenPullRequests.ResetFilters();
        MergedPullRequests.ResetFilters();
    }

    /// <summary>
    /// Releases grid state resources.
    /// </summary>
    public void Dispose()
    {
        OpenPullRequests.Dispose();
        MergedPullRequests.Dispose();
    }
}
