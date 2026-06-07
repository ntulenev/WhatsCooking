using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace WhatsCooking.Services;

/// <summary>
/// Loads and saves user preferences between application launches.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Service is created by dependency injection.")]
internal sealed class UserPreferencesService : IUserPreferencesService, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserPreferencesService"/> class.
    /// </summary>
    /// <param name="logger">Application logger.</param>
    /// <param name="timeProvider">Time provider used to delay writes.</param>
    public UserPreferencesService(ILogger<UserPreferencesService> logger, TimeProvider timeProvider)
        : this(
            logger,
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WhatsCooking",
                "preferences.json"),
            timeProvider)
    {
    }

    internal UserPreferencesService(
        ILogger<UserPreferencesService> logger,
        string preferencesPath,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferencesPath);

        _logger = logger;
        _preferencesPath = preferencesPath;
        _saveTimer = (timeProvider ?? TimeProvider.System).CreateTimer(
            SavePendingPreferences,
            null,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);
    }

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
            _logger.LogWarning("Could not read user preferences from {PreferencesPath}.", _preferencesPath);
            return new UserPreferences();
        }
        catch (JsonException)
        {
            _logger.LogWarning("User preferences at {PreferencesPath} contain invalid JSON.", _preferencesPath);
            return new UserPreferences();
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Access to user preferences at {PreferencesPath} was denied.", _preferencesPath);
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

        var json = JsonSerializer.Serialize(preferences, _serializerOptions);
        lock (_syncRoot)
        {
            _pendingJson = json;
            _ = _saveTimer.Change(_saveDelay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _ = _saveTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        SavePendingPreferences(null);
        _saveTimer.Dispose();
    }

    private void SavePendingPreferences(object? state)
    {
        string? json;
        lock (_syncRoot)
        {
            json = _pendingJson;
            _pendingJson = null;
        }

        if (json is null)
        {
            return;
        }

        var temporaryPath = _preferencesPath + ".tmp";
        try
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(_preferencesPath)!);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _preferencesPath, true);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not save user preferences to {PreferencesPath}.", _preferencesPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access to user preferences at {PreferencesPath} was denied.", _preferencesPath);
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    private static readonly TimeSpan _saveDelay = TimeSpan.FromMilliseconds(250);

    private readonly ILogger<UserPreferencesService> _logger;

    private readonly string _preferencesPath;

    private readonly ITimer _saveTimer;

    private readonly object _syncRoot = new();

    private string? _pendingJson;
}
