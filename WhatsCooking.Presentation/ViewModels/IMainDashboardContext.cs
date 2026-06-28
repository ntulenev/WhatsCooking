namespace WhatsCooking.ViewModels;

/// <summary>
/// Groups dashboard collaborators owned by the main view model.
/// </summary>
internal interface IMainDashboardContext
{
    /// <summary>
    /// Gets persisted main dashboard preferences.
    /// </summary>
    MainViewModelPreferences Preferences { get; }

    /// <summary>
    /// Gets pull request grid state.
    /// </summary>
    PullRequestDashboardViewState DashboardState { get; }

    /// <summary>
    /// Gets dashboard load command handler.
    /// </summary>
    DashboardLoadCommandHandler DashboardLoader { get; }
}
