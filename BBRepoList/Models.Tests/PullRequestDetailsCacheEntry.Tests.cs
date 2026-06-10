using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class PullRequestDetailsCacheEntryTests
{
    [Fact(DisplayName = "Pull request details cache entry preserves values")]
    [Trait("Category", "Unit")]
    public void PullRequestDetailsCacheEntryWhenCreatedPreservesValues()
    {
        var id = new PullRequestId(5);
        var firstActivityOn = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var lastActivityOn = firstActivityOn.AddHours(1);

        var entry = new PullRequestDetailsCacheEntry(
            id,
            "fingerprint",
            firstActivityOn,
            lastActivityOn,
            true,
            2);

        entry.PullRequestId.Should().Be(id);
        entry.Fingerprint.Should().Be("fingerprint");
        entry.FirstNonAuthorActivityOn.Should().Be(firstActivityOn);
        entry.LastActivityOn.Should().Be(lastActivityOn);
        entry.HasCurrentUserDiscussion.Should().BeTrue();
        entry.CommentsCount.Should().Be(2);
    }
}
