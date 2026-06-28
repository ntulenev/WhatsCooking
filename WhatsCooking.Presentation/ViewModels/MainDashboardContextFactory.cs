using System.Diagnostics.CodeAnalysis;

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
    /// <param name="createContext">Dashboard context factory delegate.</param>
    public MainDashboardContextFactory(MainDashboardContextFactoryDelegate createContext)
    {
        ArgumentNullException.ThrowIfNull(createContext);

        _createContext = createContext;
    }

    /// <inheritdoc />
    public IMainDashboardContext Create(Func<string> getGlobalSearch, ITelemetryDashboard telemetryDashboard)
    {
        ArgumentNullException.ThrowIfNull(getGlobalSearch);
        ArgumentNullException.ThrowIfNull(telemetryDashboard);

        return _createContext(getGlobalSearch, telemetryDashboard);
    }

    private readonly MainDashboardContextFactoryDelegate _createContext;
}
