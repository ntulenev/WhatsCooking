using BBRepoList.Models;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Services;

/// <summary>
/// User preferences persisted outside appsettings.
/// </summary>
internal sealed record UserPreferences
{
    /// <summary>
    /// Gets a value indicating whether the light UI theme is enabled.
    /// </summary>
    public bool IsLightTheme { get; init; }

    /// <summary>
    /// Gets the selected UI theme mode.
    /// </summary>
    public AppThemeMode? ThemeMode { get; init; }

    /// <summary>
    /// Gets the repository search phrase used for loading data.
    /// </summary>
    public string? SearchPhrase { get; init; }

    /// <summary>
    /// Gets the repository search mode used for loading data.
    /// </summary>
    public RepositorySearchMode? SearchMode { get; init; }

    /// <summary>
    /// Gets the UI scale multiplier.
    /// </summary>
    public double? UiScale { get; init; }
}
