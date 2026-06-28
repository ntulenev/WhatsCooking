namespace WhatsCooking.ViewModels;

/// <summary>
/// Creates dashboard collaborators from runtime main dashboard dependencies.
/// </summary>
/// <param name="getGlobalSearch">Returns the current global search text.</param>
/// <param name="telemetryDashboard">Telemetry dashboard state.</param>
/// <returns>Created dashboard context.</returns>
internal delegate IMainDashboardContext MainDashboardContextFactoryDelegate(
    Func<string> getGlobalSearch,
    ITelemetryDashboard telemetryDashboard);
