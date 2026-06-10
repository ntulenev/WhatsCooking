using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class PullRequestActivitySummaryTests
{
    [Fact(DisplayName = "Pull request activity summary preserves values")]
    [Trait("Category", "Unit")]
    public void PullRequestActivitySummaryWhenCreatedPreservesValues()
    {
        var firstActivityOn = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var lastActivityOn = firstActivityOn.AddHours(1);

        var summary = new PullRequestActivitySummary(firstActivityOn, lastActivityOn, true, 2);

        summary.FirstNonAuthorActivityOn.Should().Be(firstActivityOn);
        summary.LastActivityOn.Should().Be(lastActivityOn);
        summary.HasCurrentUserDiscussion.Should().BeTrue();
        summary.CommentsCount.Should().Be(2);
    }
}
