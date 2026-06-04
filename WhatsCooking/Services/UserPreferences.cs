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
}
