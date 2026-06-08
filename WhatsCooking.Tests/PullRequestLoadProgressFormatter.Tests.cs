using FluentAssertions;

using WhatsCooking.Services;
using WhatsCooking.ViewModels;

namespace WhatsCooking.Tests;

public sealed class PullRequestLoadProgressFormatterTests
{
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
