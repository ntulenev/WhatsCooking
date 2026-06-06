using WhatsCooking.ViewModels;

using Xunit;

namespace WhatsCooking.Tests;

public sealed class MergedPullRequestPeriodTests
{
    [Theory]
    [InlineData("1", 1)]
    [InlineData("365", 365)]
    [InlineData("30", 30)]
    public void TryParseAcceptsSupportedWholeNumbers(string input, int expected)
    {
        var parsed = MergedPullRequestPeriod.TryParse(input, out var actual);

        Assert.True(parsed);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("366")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("text")]
    public void TryParseRejectsUnsupportedValues(string input)
    {
        Assert.False(MergedPullRequestPeriod.TryParse(input, out _));
    }
}
