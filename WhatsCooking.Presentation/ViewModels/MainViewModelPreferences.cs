using BBRepoList.Models;

using WhatsCooking.Services;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Maintains persisted preferences used by the main dashboard view model.
/// </summary>
internal sealed class MainViewModelPreferences
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModelPreferences"/> class.
    /// </summary>
    /// <param name="preferencesService">User preferences persistence service.</param>
    public MainViewModelPreferences(IUserPreferencesService preferencesService)
    {
        ArgumentNullException.ThrowIfNull(preferencesService);

        _preferencesService = preferencesService;
        _preferences = _preferencesService.Load();
    }

    /// <summary>
    /// Gets the restored light theme setting.
    /// </summary>
    public bool IsLightTheme => _preferences.IsLightTheme;

    /// <summary>
    /// Gets the restored theme mode.
    /// </summary>
    public AppThemeMode ThemeMode =>
        _preferences.ThemeMode ?? (_preferences.IsLightTheme ? AppThemeMode.Light : AppThemeMode.Dark);

    /// <summary>
    /// Gets the restored UI scale.
    /// </summary>
    public double UiScale => NormalizeUiScale(_preferences.UiScale);

    /// <summary>
    /// Gets the restored repository search mode.
    /// </summary>
    public RepositorySearchMode SearchMode => _preferences.SearchMode ?? RepositorySearchMode.StartWith;

    /// <summary>
    /// Gets the restored repository search phrase.
    /// </summary>
    public string SearchPhrase => _preferences.SearchPhrase ?? string.Empty;

    /// <summary>
    /// Saves the light theme setting.
    /// </summary>
    /// <param name="isLightTheme">Whether light theme is enabled.</param>
    public void SaveTheme(bool isLightTheme)
    {
        _preferences = _preferences with { IsLightTheme = isLightTheme };
        _preferencesService.Save(_preferences);
    }

    /// <summary>
    /// Saves the selected theme mode.
    /// </summary>
    /// <param name="themeMode">Theme mode to persist.</param>
    public void SaveTheme(AppThemeMode themeMode)
    {
        _preferences = _preferences with
        {
            IsLightTheme = themeMode == AppThemeMode.Light,
            ThemeMode = themeMode
        };
        _preferencesService.Save(_preferences);
    }

    /// <summary>
    /// Saves the UI scale setting.
    /// </summary>
    /// <param name="uiScale">UI scale to normalize and save.</param>
    /// <returns>Normalized UI scale.</returns>
    public void SaveUiScale(double uiScale)
    {
        _preferences = _preferences with { UiScale = uiScale };
        _preferencesService.Save(_preferences);
    }

    /// <summary>
    /// Saves repository search settings used for loading dashboard data.
    /// </summary>
    /// <param name="searchMode">Repository search mode.</param>
    /// <param name="searchPhrase">Repository search phrase.</param>
    public void SaveLoadPreferences(RepositorySearchMode searchMode, string searchPhrase)
    {
        _preferences = _preferences with
        {
            SearchMode = searchMode,
            SearchPhrase = searchPhrase
        };
        _preferencesService.Save(_preferences);
    }

    public static double NormalizeUiScale(double? value)
    {
        if (value is null || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
        {
            return DEFAULT_UI_SCALE;
        }

        return Math.Round(Math.Clamp(value.Value, MIN_UI_SCALE, MAX_UI_SCALE), 2);
    }

    private const double DEFAULT_UI_SCALE = 1.0;

    private const double MIN_UI_SCALE = 0.75;

    private const double MAX_UI_SCALE = 1.5;

    private readonly IUserPreferencesService _preferencesService;

    private UserPreferences _preferences;
}
