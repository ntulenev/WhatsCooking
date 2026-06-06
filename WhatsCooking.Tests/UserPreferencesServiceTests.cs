using Microsoft.Extensions.Logging.Abstractions;

using WhatsCooking.Services;

using Xunit;

namespace WhatsCooking.Tests;

public sealed class UserPreferencesServiceTests
{
    [Fact]
    public void DisposeFlushesPendingPreferencesAtomically()
    {
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

            using var reader = new UserPreferencesService(NullLogger<UserPreferencesService>.Instance, preferencesPath);
            var preferences = reader.Load();

            Assert.True(preferences.IsLightTheme);
            Assert.Equal("platform", preferences.SearchPhrase);
            Assert.Equal(1.2, preferences.UiScale);
            Assert.False(File.Exists(preferencesPath + ".tmp"));
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
