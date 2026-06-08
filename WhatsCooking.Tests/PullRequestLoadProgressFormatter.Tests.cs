using FluentAssertions;

using WhatsCooking.Services;
using WhatsCooking.ViewModels;

namespace WhatsCooking.Tests;

public sealed class PullRequestLoadProgressFormatterTests
{
    [Theory(DisplayName = "Format returns text for every load stage")]
    [Trait("Category", "Unit")]
    [InlineData(0, "Loading demo data")]
    [InlineData(1, "Authenticating")]
    [InlineData(2, "Loading repositories")]
    [InlineData(3, "Scanning repositories for open pull requests")]
    [InlineData(4, "Scanning repositories for merged pull requests")]
    [InlineData(5, "Completed")]
    public void FormatWhenStageIsKnownReturnsStageText(int stageValue, string expected)
    {
        // Arrange
        var progress = new PullRequestLoadProgress((PullRequestLoadStage)stageValue);

        // Act
        var result = PullRequestLoadProgressFormatter.Format(progress);

        // Assert
        result.Should().Be(expected);
    }

    [Fact(DisplayName = "Format throws when progress is null")]
    [Trait("Category", "Unit")]
    public void FormatWhenProgressIsNullThrowsArgumentNullException()
    {
        // Arrange
        PullRequestLoadProgress progress = null!;

        // Act
        Action act = () => PullRequestLoadProgressFormatter.Format(progress);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Format throws when stage is unknown")]
    [Trait("Category", "Unit")]
    public void FormatWhenStageIsUnknownThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var progress = new PullRequestLoadProgress((PullRequestLoadStage)999);

        // Act
        Action act = () => PullRequestLoadProgressFormatter.Format(progress);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Format includes counts when progress contains counts")]
    [Trait("Category", "Unit")]
    public void FormatIncludesCountsWhenAvailable()
    {
        // Arrange
        var progress = new PullRequestLoadProgress(
            PullRequestLoadStage.LoadingRepositories,
            3,
            10);

        // Act
        var result = PullRequestLoadProgressFormatter.Format(progress);

        // Assert
        result.Should().Be("Loading repositories: 3/10");
    }
}
