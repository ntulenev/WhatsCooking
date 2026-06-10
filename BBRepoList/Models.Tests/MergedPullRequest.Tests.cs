using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class MergedPullRequestTests
{
    [Fact(DisplayName = "Merged pull request maps merge calculations")]
    [Trait("Category", "Unit")]
    public void MergedPullRequestWhenCreatedMapsMergeCalculations()
    {
        var openedOn = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var mergedOn = openedOn.AddDays(2);
        var repository = new Repository("Repository", slug: new RepositorySlug("repo"));
        var authorId = new BitbucketId("author");
        var firstActivityOn = openedOn.AddHours(1);
        var lastActivityOn = mergedOn.AddHours(-1);

        var pullRequest = new MergedPullRequest(
            repository,
            new PullRequestId(7),
            " Title ",
            openedOn,
            authorId,
            " Author ",
            firstActivityOn,
            lastActivityOn,
            hasCurrentUserDiscussion: true,
            mergedOn,
            descriptionText: " Description ",
            commentsCount: 3,
            requestChangesCount: 1,
            hasCurrentUserRequestChanges: true,
            approvalsCount: 1,
            hasCurrentUserApproval: true);

        pullRequest.Repository.Should().BeSameAs(repository);
        pullRequest.RepositoryName.Should().Be("Repository");
        pullRequest.RepositorySlug.Should().Be(new RepositorySlug("repo"));
        pullRequest.PullRequestId.Should().Be(new PullRequestId(7));
        pullRequest.Title.Should().Be("Title");
        pullRequest.OpenedOn.Should().Be(openedOn);
        pullRequest.AuthorId.Should().Be(authorId);
        pullRequest.AuthorDisplayName.Should().Be("Author");
        pullRequest.FirstNonAuthorActivityOn.Should().Be(firstActivityOn);
        pullRequest.LastActivityOn.Should().Be(lastActivityOn);
        pullRequest.HasCurrentUserDiscussion.Should().BeTrue();
        pullRequest.MergedOn.Should().Be(mergedOn);
        pullRequest.DescriptionText.Should().Be("Description");
        pullRequest.CommentsCount.Should().Be(3);
        pullRequest.RequestChangesCount.Should().Be(1);
        pullRequest.HasCurrentUserRequestChanges.Should().BeTrue();
        pullRequest.ApprovalsCount.Should().Be(1);
        pullRequest.HasCurrentUserApproval.Should().BeTrue();
        pullRequest.GetOpenDuration().Should().Be(TimeSpan.FromDays(2));
        pullRequest.GetLastActivityAge(mergedOn).Should().Be(TimeSpan.FromHours(1));
        pullRequest.TimeToFirstResponse.Should().Be(TimeSpan.FromHours(1));
        pullRequest.HasCurrentUserActivity.Should().BeTrue();
        pullRequest.HasShortOrMissingDescription(12).Should().BeTrue();
        pullRequest.HasShortOrMissingDescription(11).Should().BeFalse();
    }

    [Fact(DisplayName = "Merged pull request clamps negative open duration")]
    [Trait("Category", "Unit")]
    public void MergedPullRequestWhenMergedBeforeOpeningClampsDuration()
    {
        var openedOn = new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero);

        var pullRequest = new MergedPullRequest(
            new Repository("Repository"),
            new PullRequestId(1),
            "Title",
            openedOn,
            authorId: null,
            authorDisplayName: null,
            firstNonAuthorActivityOn: null,
            lastActivityOn: null,
            hasCurrentUserDiscussion: false,
            openedOn.AddDays(-1));

        pullRequest.GetOpenDuration().Should().Be(TimeSpan.Zero);
        pullRequest.GetLastActivityAge(openedOn).Should().BeNull();
        pullRequest.TimeToFirstResponse.Should().BeNull();
        pullRequest.HasCurrentUserActivity.Should().BeFalse();
    }

    [Theory(DisplayName = "Merged pull request detects each current user activity kind")]
    [Trait("Category", "Unit")]
    [InlineData(true, 0, false, 0, false)]
    [InlineData(false, 1, true, 0, false)]
    [InlineData(false, 0, false, 1, true)]
    public void MergedPullRequestWhenCurrentUserHasActivityReturnsTrue(
        bool hasDiscussion,
        int requestChangesCount,
        bool hasRequestChanges,
        int approvalsCount,
        bool hasApproval)
    {
        var pullRequest = new MergedPullRequest(
            new Repository("Repository"),
            new PullRequestId(1),
            "Title",
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
            authorId: null,
            authorDisplayName: null,
            firstNonAuthorActivityOn: null,
            lastActivityOn: null,
            hasDiscussion,
            new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero),
            requestChangesCount: requestChangesCount,
            hasCurrentUserRequestChanges: hasRequestChanges,
            approvalsCount: approvalsCount,
            hasCurrentUserApproval: hasApproval);

        pullRequest.HasCurrentUserActivity.Should().BeTrue();
    }

    [Theory(DisplayName = "Merged pull request rejects invalid constructor values")]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void MergedPullRequestWhenValueIsInvalidThrowsArgumentException(int invalidValue)
    {
        var repository = new Repository("Repository");
        var openedOn = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

        Action act = invalidValue switch
        {
            0 => () => _ = CreatePullRequest(repository, openedOn, title: " "),
            1 => () => _ = CreatePullRequest(repository, openedOn, commentsCount: -1),
            2 => () => _ = CreatePullRequest(repository, openedOn, requestChangesCount: -1),
            _ => () => _ = CreatePullRequest(repository, openedOn, approvalsCount: -1)
        };

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Merged pull request rejects null repository")]
    [Trait("Category", "Unit")]
    public void MergedPullRequestWhenRepositoryIsNullThrowsArgumentNullException()
    {
        Action act = () => _ = CreatePullRequest(
            null!,
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Merged pull request rejects negative description threshold")]
    [Trait("Category", "Unit")]
    public void HasShortOrMissingDescriptionWhenThresholdIsNegativeThrowsArgumentOutOfRangeException()
    {
        var pullRequest = CreatePullRequest(
            new Repository("Repository"),
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));

        Action act = () => pullRequest.HasShortOrMissingDescription(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static MergedPullRequest CreatePullRequest(
        Repository repository,
        DateTimeOffset openedOn,
        string title = "Title",
        int commentsCount = 0,
        int requestChangesCount = 0,
        int approvalsCount = 0) =>
        new(
            repository,
            new PullRequestId(1),
            title,
            openedOn,
            authorId: null,
            authorDisplayName: null,
            firstNonAuthorActivityOn: null,
            lastActivityOn: null,
            hasCurrentUserDiscussion: false,
            openedOn.AddDays(1),
            commentsCount: commentsCount,
            requestChangesCount: requestChangesCount,
            approvalsCount: approvalsCount);
}
