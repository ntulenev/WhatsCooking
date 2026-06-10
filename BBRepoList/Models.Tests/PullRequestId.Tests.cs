using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class PullRequestIdTests
{
    [Theory(DisplayName = "Pull request id requires a positive value")]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(-1)]
    public void PullRequestIdWhenValueIsNotPositiveThrowsArgumentOutOfRangeException(int value)
    {
        Action act = () => _ = new PullRequestId(value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Pull request id preserves and formats value")]
    [Trait("Category", "Unit")]
    public void PullRequestIdWhenValueIsPositivePreservesAndFormatsValue()
    {
        var id = new PullRequestId(42);

        id.Value.Should().Be(42);
        id.ToString().Should().Be("42");
    }
}
