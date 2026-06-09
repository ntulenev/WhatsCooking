using FluentAssertions;

using WhatsCooking.Services;
using WhatsCooking.ViewModels;

namespace WhatsCooking.Tests;

public sealed class PresentationContractsTests
{
    [Fact(DisplayName = "Async command failed event args throws when exception is null")]
    [Trait("Category", "Unit")]
    public void AsyncCommandFailedEventArgsConstructorWhenExceptionIsNullThrowsArgumentNullException()
    {
        // Arrange
        Exception exception = null!;

        // Act
        Action act = () => _ = new AsyncCommandFailedEventArgs(exception);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Async command failed event args exposes exact exception")]
    [Trait("Category", "Unit")]
    public void AsyncCommandFailedEventArgsConstructorWhenExceptionIsValidExposesExactException()
    {
        // Arrange
        var exception = new InvalidOperationException("Failure");

        // Act
        var args = new AsyncCommandFailedEventArgs(exception);

        // Assert
        args.Exception.Should().BeSameAs(exception);
    }

    [Fact(DisplayName = "User preferences default to empty state")]
    [Trait("Category", "Unit")]
    public void UserPreferencesConstructorWhenCalledCreatesEmptyState()
    {
        // Act
        var preferences = new UserPreferences();

        // Assert
        preferences.Should().BeEquivalentTo(new
        {
            IsLightTheme = false,
            SearchPhrase = (string?)null,
            SearchMode = (BBRepoList.Models.RepositorySearchMode?)null,
            UiScale = (double?)null
        });
    }
}
