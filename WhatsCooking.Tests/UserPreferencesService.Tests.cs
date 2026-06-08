using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using WhatsCooking.Services;

namespace WhatsCooking.Tests;

public sealed class UserPreferencesServiceTests
{
    [Fact(DisplayName = "Dispose flushes pending preferences atomically")]
    [Trait("Category", "Integration")]
    public void DisposeFlushesPendingPreferencesAtomically()
    {
        // Arrange
        var testDirectory = Path.Combine(Path.GetTempPath(), "WhatsCooking.Tests", Guid.NewGuid().ToString("N"));
        var preferencesPath = Path.Combine(testDirectory, "preferences.json");
        try
        {
            using (var service = new UserPreferencesService(NullLogger<UserPreferencesService>.Instance, preferencesPath))
            {
                service.Save(new UserPreferences
                {
                    IsLightTheme = true,
                    SearchPhrase = "platform",
                    UiScale = 1.2
                });
            }

            // Act
            using var reader = new UserPreferencesService(NullLogger<UserPreferencesService>.Instance, preferencesPath);
            var preferences = reader.Load();

            // Assert
            preferences.IsLightTheme.Should().BeTrue();
            preferences.SearchPhrase.Should().Be("platform");
            preferences.UiScale.Should().Be(1.2);
            File.Exists(preferencesPath + ".tmp").Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }
}
