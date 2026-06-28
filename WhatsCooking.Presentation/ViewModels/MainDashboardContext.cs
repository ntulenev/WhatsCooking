namespace WhatsCooking.ViewModels;

/// <summary>
/// Groups dashboard collaborators owned by the main view model.
/// </summary>
/// <param name="Preferences">Persisted main dashboard preferences.</param>
/// <param name="DashboardState">Pull request grid state.</param>
/// <param name="DashboardLoader">Dashboard load command handler.</param>
internal sealed record MainDashboardContext(
    MainViewModelPreferences Preferences,
    PullRequestDashboardViewState DashboardState,
    DashboardLoadCommandHandler DashboardLoader) : IMainDashboardContext;
