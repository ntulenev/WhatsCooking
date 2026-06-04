using BBRepoList.Models;

namespace WhatsCooking.Services;

/// <summary>
/// User preferences persisted outside appsettings.
/// </summary>
internal sealed class UserPreferences
{
    /// <summary>
    /// Gets or sets a value indicating whether the light UI theme is enabled.
    /// </summary>
    public bool IsLightTheme { get; set; }

    /// <summary>
    /// Gets or sets the repository search phrase used for loading data.
    /// </summary>
    public string? SearchPhrase { get; set; }

    /// <summary>
    /// Gets or sets the repository search mode used for loading data.
    /// </summary>
    public RepositorySearchMode? SearchMode { get; set; }

    /// <summary>
    /// Gets or sets the UI scale multiplier.
    /// </summary>
    public double? UiScale { get; set; }
}
