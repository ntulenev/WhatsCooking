using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using WhatsCooking.Services;

namespace WhatsCooking.Infrastructure.Tests;

public sealed class UserPreferencesServiceTests
{
    [Fact(DisplayName = "Constructor throws when logger is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenLoggerIsNullThrowsArgumentNullException()
    {
        // Arrange
        Microsoft.Extensions.Logging.ILogger<UserPreferencesService> logger = null!;

        // Act
        Action act = () => _ = new UserPreferencesService(logger, "preferences.json");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory(DisplayName = "Constructor throws when preferences path is invalid")]
    [Trait("Category", "Unit")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorWhenPreferencesPathIsInvalidThrowsArgumentException(string? preferencesPath)
    {
        // Act
        Action act = () => _ = new UserPreferencesService(
            NullLogger<UserPreferencesService>.Instance,
            preferencesPath!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Load returns defaults when preferences file does not exist")]
    [Trait("Category", "Integration")]
    public void LoadWhenPreferencesFileDoesNotExistReturnsDefaults()
    {
        // Arrange
        var preferencesPath = Path.Combine(
            Path.GetTempPath(),
            "WhatsCooking.Infrastructure.Tests",
            Guid.NewGuid().ToString("N"),
            "preferences.json");
        using var service = new UserPreferencesService(
            NullLogger<UserPreferencesService>.Instance,
            preferencesPath);

        // Act
        var result = service.Load();

        // Assert
        result.Should().BeEquivalentTo(new UserPreferences());
    }

    [Fact(DisplayName = "Load returns defaults when preferences JSON is invalid")]
    [Trait("Category", "Integration")]
    public void LoadWhenPreferencesJsonIsInvalidReturnsDefaults()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var preferencesPath = Path.Combine(directory.Path, "preferences.json");
        File.WriteAllText(preferencesPath, "{ invalid json");
        using var service = new UserPreferencesService(
            NullLogger<UserPreferencesService>.Instance,
            preferencesPath);

        // Act
        var result = service.Load();

        // Assert
        result.Should().BeEquivalentTo(new UserPreferences());
    }

    [Fact(DisplayName = "Save throws when preferences are null")]
    [Trait("Category", "Unit")]
    public void SaveWhenPreferencesAreNullThrowsArgumentNullException()
    {
        // Arrange
        using var service = new UserPreferencesService(
            NullLogger<UserPreferencesService>.Instance,
            "preferences.json");
        UserPreferences preferences = null!;

        // Act
        Action act = () => service.Save(preferences);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Dispose flushes pending preferences atomically")]
    [Trait("Category", "Integration")]
    public void DisposeFlushesPendingPreferencesAtomically()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var preferencesPath = Path.Combine(directory.Path, "preferences.json");
        using (var service = new UserPreferencesService(
            NullLogger<UserPreferencesService>.Instance,
            preferencesPath))
        {
            service.Save(new UserPreferences
            {
                IsLightTheme = true,
                SearchPhrase = "platform",
                UiScale = 1.2
            });
        }

        // Act
        using var reader = new UserPreferencesService(
            NullLogger<UserPreferencesService>.Instance,
            preferencesPath);
        var preferences = reader.Load();

        // Assert
        preferences.IsLightTheme.Should().BeTrue();
        preferences.SearchPhrase.Should().Be("platform");
        preferences.UiScale.Should().Be(1.2);
        File.Exists(preferencesPath + ".tmp").Should().BeFalse();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "WhatsCooking.Infrastructure.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
