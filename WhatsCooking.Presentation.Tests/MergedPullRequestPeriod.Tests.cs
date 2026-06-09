using FluentAssertions;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Presentation.Tests;

public sealed class MergedPullRequestPeriodTests
{
    [Theory(DisplayName = "TryParse accepts supported whole numbers")]
    [Trait("Category", "Unit")]
    [InlineData("1", 1)]
    [InlineData("365", 365)]
    [InlineData("30", 30)]
    public void TryParseAcceptsSupportedWholeNumbers(string input, int expected)
    {
        // Act
        var parsed = MergedPullRequestPeriod.TryParse(input, out var actual);

        // Assert
        parsed.Should().BeTrue();
        actual.Should().Be(expected);
    }

    [Theory(DisplayName = "TryParse rejects unsupported values")]
    [Trait("Category", "Unit")]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("366")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("text")]
    public void TryParseRejectsUnsupportedValues(string input)
    {
        // Act
        var parsed = MergedPullRequestPeriod.TryParse(input, out _);

        // Assert
        parsed.Should().BeFalse();
    }
}
