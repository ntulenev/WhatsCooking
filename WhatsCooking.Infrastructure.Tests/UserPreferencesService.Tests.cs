using BBRepoList.Models;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using WhatsCooking.Services;

namespace WhatsCooking.Infrastructure.Tests;

public sealed class UserPreferencesServiceTests
{
    [Fact(DisplayName = "Path constructor throws when logger is null")]
    [Trait("Category", "Unit")]
    public void PathConstructorWhenLoggerIsNullThrowsArgumentNullException()
    {
        // Arrange
        Microsoft.Extensions.Logging.ILogger<UserPreferencesService> logger = null!;

        // Act
        Action act = () => _ = new UserPreferencesService(logger, "preferences.json");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Public constructor throws when logger is null")]
    [Trait("Category", "Unit")]
    public void PublicConstructorWhenLoggerIsNullThrowsArgumentNullException()
    {
        // Arrange
        Microsoft.Extensions.Logging.ILogger<UserPreferencesService> logger = null!;

        // Act
        Action act = () => _ = new UserPreferencesService(logger, TimeProvider.System);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Public constructor throws when time provider is null")]
    [Trait("Category", "Unit")]
    public void PublicConstructorWhenTimeProviderIsNullThrowsArgumentNullException()
    {
        // Arrange
        TimeProvider timeProvider = null!;

        // Act
        Action act = () => _ = new UserPreferencesService(
            NullLogger<UserPreferencesService>.Instance,
            timeProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Public constructor creates timer with infinite schedule")]
    [Trait("Category", "Unit")]
    public void PublicConstructorWhenDependenciesAreValidCreatesTimerWithInfiniteSchedule()
    {
        // Arrange
        var fixture = CreateTimerFixture();

        // Act
        using var service = new UserPreferencesService(
            NullLogger<UserPreferencesService>.Instance,
            fixture.TimeProvider.Object);

        // Assert
        fixture.TimeProvider.Verify(instance => instance.CreateTimer(
            It.IsAny<TimerCallback>(),
            null,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan), Times.Once);
    }

    [Theory(DisplayName = "Path constructor throws when preferences path is invalid")]
    [Trait("Category", "Unit")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PathConstructorWhenPreferencesPathIsInvalidThrowsArgumentException(string? preferencesPath)
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

    [Fact(DisplayName = "Load returns defaults when preferences JSON contains null")]
    [Trait("Category", "Integration")]
    public void LoadWhenPreferencesJsonContainsNullReturnsDefaults()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var preferencesPath = Path.Combine(directory.Path, "preferences.json");
        File.WriteAllText(preferencesPath, "null");
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

    [Fact(DisplayName = "Save schedules pending preferences with configured delay")]
    [Trait("Category", "Integration")]
    public void SaveWhenPreferencesAreValidSchedulesPendingWrite()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var fixture = CreateTimerFixture();
        using var service = new UserPreferencesService(
            NullLogger<UserPreferencesService>.Instance,
            Path.Combine(directory.Path, "preferences.json"),
            fixture.TimeProvider.Object);
        var preferences = new UserPreferences { SearchPhrase = "platform" };

        // Act
        service.Save(preferences);

        // Assert
        fixture.Timer.Verify(instance => instance.Change(
            TimeSpan.FromMilliseconds(250),
            Timeout.InfiniteTimeSpan), Times.Once);
    }

    [Fact(DisplayName = "Timer callback persists pending preferences")]
    [Trait("Category", "Integration")]
    public void TimerCallbackWhenPreferencesArePendingPersistsPreferences()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var preferencesPath = Path.Combine(directory.Path, "preferences.json");
        var fixture = CreateTimerFixture();
        using var service = new UserPreferencesService(
            NullLogger<UserPreferencesService>.Instance,
            preferencesPath,
            fixture.TimeProvider.Object);
        service.Save(new UserPreferences
        {
            SearchPhrase = "platform",
            SearchMode = RepositorySearchMode.Contains
        });

        // Act
        fixture.Callback(null);
        var result = service.Load();

        // Assert
        result.SearchPhrase.Should().Be("platform");
        result.SearchMode.Should().Be(RepositorySearchMode.Contains);
        File.Exists(preferencesPath + ".tmp").Should().BeFalse();
    }

    [Fact(DisplayName = "Dispose without pending preferences stops and disposes timer")]
    [Trait("Category", "Unit")]
    public void DisposeWhenNoPreferencesArePendingStopsAndDisposesTimer()
    {
        // Arrange
        var fixture = CreateTimerFixture();
        var service = new UserPreferencesService(
            NullLogger<UserPreferencesService>.Instance,
            "preferences.json",
            fixture.TimeProvider.Object);

        // Act
        service.Dispose();

        // Assert
        fixture.Timer.Verify(instance => instance.Change(
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan), Times.Once);
        fixture.Timer.Verify(instance => instance.Dispose(), Times.Once);
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

    [Fact(DisplayName = "Dispose persists only latest pending preferences")]
    [Trait("Category", "Integration")]
    public void DisposeWhenSaveIsCalledMultipleTimesPersistsLatestPreferences()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var preferencesPath = Path.Combine(directory.Path, "preferences.json");
        using (var service = new UserPreferencesService(
            NullLogger<UserPreferencesService>.Instance,
            preferencesPath))
        {
            service.Save(new UserPreferences { SearchPhrase = "first" });
            service.Save(new UserPreferences
            {
                IsLightTheme = true,
                SearchPhrase = "latest",
                SearchMode = RepositorySearchMode.StartWith,
                UiScale = 1.35
            });
        }

        // Act
        using var reader = new UserPreferencesService(
            NullLogger<UserPreferencesService>.Instance,
            preferencesPath);
        var result = reader.Load();

        // Assert
        result.Should().BeEquivalentTo(new UserPreferences
        {
            IsLightTheme = true,
            SearchPhrase = "latest",
            SearchMode = RepositorySearchMode.StartWith,
            UiScale = 1.35
        });
    }

    private static TimerFixture CreateTimerFixture()
    {
        TimerCallback? callback = null;
        var timer = new Mock<ITimer>(MockBehavior.Strict);
        timer.Setup(instance => instance.Change(
                TimeSpan.FromMilliseconds(250),
                Timeout.InfiniteTimeSpan))
            .Returns(true);
        timer.Setup(instance => instance.Change(
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan))
            .Returns(true);
        timer.Setup(instance => instance.Dispose());

        var timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(instance => instance.CreateTimer(
                It.IsAny<TimerCallback>(),
                null,
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan))
            .Callback<TimerCallback, object?, TimeSpan, TimeSpan>((value, _, _, _) => callback = value)
            .Returns(timer.Object);

        return new TimerFixture(
            timeProvider,
            timer,
            state => callback!(state));
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

    private sealed record TimerFixture(
        Mock<TimeProvider> TimeProvider,
        Mock<ITimer> Timer,
        TimerCallback Callback);
}
