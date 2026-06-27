using System.Diagnostics.CodeAnalysis;

using WhatsCooking.Services;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Creates dashboard collaborators for the main view model.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Factory is created by dependency injection.")]
internal sealed class MainDashboardContextFactory : IMainDashboardContextFactory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainDashboardContextFactory"/> class.
    /// </summary>
    /// <param name="loadCoordinator">Dashboard load coordinator.</param>
    /// <param name="rowMapper">Pull request row mapper.</param>
    /// <param name="dialogService">User-facing dialog service.</param>
    /// <param name="preferencesService">User preferences persistence service.</param>
    public MainDashboardContextFactory(
        IDashboardLoadCoordinator loadCoordinator,
        IPullRequestRowMapper rowMapper,
        IDialogService dialogService,
        IUserPreferencesService preferencesService)
    {
        ArgumentNullException.ThrowIfNull(loadCoordinator);
        ArgumentNullException.ThrowIfNull(rowMapper);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(preferencesService);

        _loadCoordinator = loadCoordinator;
        _rowMapper = rowMapper;
        _dialogService = dialogService;
        _preferencesService = preferencesService;
    }

    /// <inheritdoc />
    public MainDashboardContext Create(Func<string> getGlobalSearch, ITelemetryDashboard telemetryDashboard)
    {
        ArgumentNullException.ThrowIfNull(getGlobalSearch);
        ArgumentNullException.ThrowIfNull(telemetryDashboard);

        var preferences = new MainViewModelPreferences(_preferencesService);
        var dashboardState = new PullRequestDashboardViewState(getGlobalSearch);
        var dashboardLoader = new DashboardLoadCommandHandler(
            _loadCoordinator,
            _rowMapper,
            _dialogService,
            preferences,
            dashboardState,
            telemetryDashboard);

        return new MainDashboardContext(preferences, dashboardState, dashboardLoader);
    }

    private readonly IDashboardLoadCoordinator _loadCoordinator;

    private readonly IPullRequestRowMapper _rowMapper;

    private readonly IDialogService _dialogService;

    private readonly IUserPreferencesService _preferencesService;
}
