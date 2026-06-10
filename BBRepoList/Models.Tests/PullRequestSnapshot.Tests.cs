using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class PullRequestSnapshotTests
{
    [Fact(DisplayName = "Pull request snapshot preserves values")]
    [Trait("Category", "Unit")]
    public void PullRequestSnapshotWhenCreatedPreservesValues()
    {
        var id = new PullRequestId(5);
        var createdOn = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        var snapshot = new PullRequestSnapshot(
            id,
            "Title",
            createdOn,
            DescriptionText: "Description",
            AuthorId: new BitbucketId("author"),
            AuthorDisplayName: "Author",
            RequestChangesCount: 1,
            HasCurrentUserRequestChanges: true,
            ApprovalsCount: 2,
            HasCurrentUserApproval: true,
            CacheFingerprint: "fingerprint");

        snapshot.Id.Should().Be(id);
        snapshot.Title.Should().Be("Title");
        snapshot.CreatedOn.Should().Be(createdOn);
        snapshot.DescriptionText.Should().Be("Description");
        snapshot.AuthorId.Should().Be(new BitbucketId("author"));
        snapshot.AuthorDisplayName.Should().Be("Author");
        snapshot.RequestChangesCount.Should().Be(1);
        snapshot.HasCurrentUserRequestChanges.Should().BeTrue();
        snapshot.ApprovalsCount.Should().Be(2);
        snapshot.HasCurrentUserApproval.Should().BeTrue();
        snapshot.CacheFingerprint.Should().Be("fingerprint");
    }
}
