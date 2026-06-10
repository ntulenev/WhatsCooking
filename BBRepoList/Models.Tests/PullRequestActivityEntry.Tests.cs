using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class PullRequestActivityEntryTests
{
    [Fact(DisplayName = "Pull request activity entry preserves values")]
    [Trait("Category", "Unit")]
    public void PullRequestActivityEntryWhenCreatedPreservesValues()
    {
        var happenedOn = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        var activity = new PullRequestActivityEntry(new BitbucketId("actor"), happenedOn, true);

        activity.ActorId.Should().Be(new BitbucketId("{ACTOR}"));
        activity.HappenedOn.Should().Be(happenedOn);
        activity.IsComment.Should().BeTrue();
    }
}
