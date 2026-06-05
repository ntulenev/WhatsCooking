using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;

namespace WhatsCooking.Services;

/// <summary>
/// Loads and saves user preferences between application launches.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Service is created by dependency injection.")]
internal sealed class UserPreferencesService : IUserPreferencesService
{
    /// <summary>
    /// Loads persisted user preferences.
    /// </summary>
    /// <returns>Stored preferences, or defaults when no preferences exist.</returns>
    public UserPreferences Load()
    {
        if (!File.Exists(_preferencesPath))
        {
            return new UserPreferences();
        }

        try
        {
            var json = File.ReadAllText(_preferencesPath);
            return JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
        }
        catch (IOException)
        {
            return new UserPreferences();
        }
        catch (JsonException)
        {
            return new UserPreferences();
        }
        catch (UnauthorizedAccessException)
        {
            return new UserPreferences();
        }
    }

    /// <summary>
    /// Saves user preferences.
    /// </summary>
    /// <param name="preferences">Preferences to persist.</param>
    public void Save(UserPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences, nameof(preferences));

        try
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(_preferencesPath)!);
            var json = JsonSerializer.Serialize(preferences, _serializerOptions);
            File.WriteAllText(_preferencesPath, json);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _preferencesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WhatsCooking",
        "preferences.json");
}
