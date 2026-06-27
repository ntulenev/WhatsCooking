namespace WhatsCooking.ViewModels;

/// <summary>
/// Creates the dashboard context used by the main view model.
/// </summary>
internal interface IMainDashboardContextFactory
{
    /// <summary>
    /// Creates the dashboard context.
    /// </summary>
    /// <param name="getGlobalSearch">Returns the current global search text.</param>
    /// <param name="telemetryDashboard">Telemetry dashboard state.</param>
    /// <returns>Created dashboard context.</returns>
    MainDashboardContext Create(Func<string> getGlobalSearch, ITelemetryDashboard telemetryDashboard);
}
