using BBRepoList.Models;

using FluentAssertions;

using Moq;

using WhatsCooking.Services;
using WhatsCooking.ViewModels;

namespace WhatsCooking.Presentation.Tests;

public sealed class MainViewModelPreferencesTests
{
    [Fact(DisplayName = "Constructor restores normalized preferences")]
    [Trait("Category", "Unit")]
    public void ConstructorRestoresNormalizedPreferences()
    {
        // Arrange
        var service = new Mock<IUserPreferencesService>(MockBehavior.Strict);
        service.Setup(instance => instance.Load())
            .Returns(new UserPreferences
            {
                IsLightTheme = true,
                SearchMode = RepositorySearchMode.Contains,
                SearchPhrase = "pay",
                UiScale = 10
            });

        // Act
        var preferences = new MainViewModelPreferences(service.Object);

        // Assert
        preferences.IsLightTheme.Should().BeTrue();
        preferences.SearchMode.Should().Be(RepositorySearchMode.Contains);
        preferences.SearchPhrase.Should().Be("pay");
        preferences.UiScale.Should().Be(1.5);
        service.VerifyAll();
    }

    [Fact(DisplayName = "Constructor restores defaults from empty preferences")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPreferencesAreEmptyRestoresDefaults()
    {
        // Arrange
        var service = new Mock<IUserPreferencesService>(MockBehavior.Strict);
        service.Setup(instance => instance.Load()).Returns(new UserPreferences());

        // Act
        var preferences = new MainViewModelPreferences(service.Object);

        // Assert
        preferences.IsLightTheme.Should().BeFalse();
        preferences.SearchMode.Should().Be(RepositorySearchMode.StartWith);
        preferences.SearchPhrase.Should().BeEmpty();
        preferences.UiScale.Should().Be(1.0);
        service.VerifyAll();
    }

    [Fact(DisplayName = "Save methods persist updated preferences")]
    [Trait("Category", "Unit")]
    public void SaveMethodsPersistUpdatedPreferences()
    {
        // Arrange
        var saved = new List<UserPreferences>();
        var service = new Mock<IUserPreferencesService>(MockBehavior.Strict);
        service.Setup(instance => instance.Load()).Returns(new UserPreferences());
        service.Setup(instance => instance.Save(It.IsAny<UserPreferences>()))
            .Callback<UserPreferences>(saved.Add);
        var preferences = new MainViewModelPreferences(service.Object);

        // Act
        preferences.SaveTheme(true);
        preferences.SaveUiScale(1.25);
        preferences.SaveLoadPreferences(RepositorySearchMode.Contains, "repo");

        // Assert
        saved.Should().Equal(
            new UserPreferences { IsLightTheme = true },
            new UserPreferences { IsLightTheme = true, UiScale = 1.25 },
            new UserPreferences
            {
                IsLightTheme = true,
                UiScale = 1.25,
                SearchMode = RepositorySearchMode.Contains,
                SearchPhrase = "repo"
            });
        service.VerifyAll();
    }

    [Theory(DisplayName = "NormalizeUiScale clamps unsupported values")]
    [Trait("Category", "Unit")]
    [InlineData(null, 1.0)]
    [InlineData(0.1, 0.75)]
    [InlineData(2.0, 1.5)]
    [InlineData(1.234, 1.23)]
    public void NormalizeUiScaleClampsUnsupportedValues(double? input, double expected)
    {
        // Act
        var actual = MainViewModelPreferences.NormalizeUiScale(input);

        // Assert
        actual.Should().Be(expected);
    }
}
