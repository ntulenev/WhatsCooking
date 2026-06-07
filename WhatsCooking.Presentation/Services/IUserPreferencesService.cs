namespace WhatsCooking.Services;

/// <summary>
/// Loads and saves user preferences between application launches.
/// </summary>
internal interface IUserPreferencesService
{
    /// <summary>
    /// Loads persisted user preferences.
    /// </summary>
    /// <returns>Stored preferences, or defaults when no preferences exist.</returns>
    UserPreferences Load();

    /// <summary>
    /// Saves user preferences.
    /// </summary>
    /// <param name="preferences">Preferences to persist.</param>
    void Save(UserPreferences preferences);
}
