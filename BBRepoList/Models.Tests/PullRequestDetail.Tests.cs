using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class PullRequestDetailTests
{
    [Fact(DisplayName = "Pull request detail maps normalized values and calculated properties")]
    [Trait("Category", "Unit")]
    public void PullRequestDetailWhenCreatedMapsValuesAndCalculations()
    {
        var openedOn = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var firstActivityOn = openedOn.AddHours(2);
        var lastActivityOn = openedOn.AddHours(5);
        var repository = new Repository("Repository", slug: new RepositorySlug("repo"));

        var detail = new PullRequestDetail(
            repository,
            new PullRequestId(42),
            " Title ",
            openedOn,
            new BitbucketId("author"),
            " Author ",
            firstActivityOn,
            lastActivityOn,
            hasCurrentUserDiscussion: true,
            descriptionText: " Description ",
            commentsCount: 3,
            requestChangesCount: 1,
            hasCurrentUserRequestChanges: true,
            approvalsCount: 2,
            hasCurrentUserApproval: true);

        detail.Title.Should().Be("Title");
        detail.Repository.Should().BeSameAs(repository);
        detail.PullRequestId.Should().Be(new PullRequestId(42));
        detail.OpenedOn.Should().Be(openedOn);
        detail.DescriptionText.Should().Be("Description");
        detail.AuthorId.Should().Be(new BitbucketId("author"));
        detail.AuthorDisplayName.Should().Be("Author");
        detail.FirstNonAuthorActivityOn.Should().Be(firstActivityOn);
        detail.LastActivityOn.Should().Be(lastActivityOn);
        detail.HasCurrentUserDiscussion.Should().BeTrue();
        detail.CommentsCount.Should().Be(3);
        detail.RequestChangesCount.Should().Be(1);
        detail.HasCurrentUserRequestChanges.Should().BeTrue();
        detail.ApprovalsCount.Should().Be(2);
        detail.HasCurrentUserApproval.Should().BeTrue();
        detail.RepositoryName.Should().Be("Repository");
        detail.RepositorySlug.Should().Be(new RepositorySlug("repo"));
        detail.RepositoryCreatedOn.Should().BeNull();
        detail.TimeToFirstResponse.Should().Be(TimeSpan.FromHours(2));
        detail.GetOpenDuration(openedOn.AddDays(1)).Should().Be(TimeSpan.FromDays(1));
        detail.GetLastActivityAge(openedOn.AddHours(8)).Should().Be(TimeSpan.FromHours(3));
        detail.HasCurrentUserActivity.Should().BeTrue();
        detail.HasShortOrMissingDescription(12).Should().BeTrue();
        detail.HasShortOrMissingDescription(11).Should().BeFalse();
    }

    [Fact(DisplayName = "Pull request detail clamps negative durations and inactive flags")]
    [Trait("Category", "Unit")]
    public void PullRequestDetailWhenActivityPrecedesOpeningClampsDurationsAndFlags()
    {
        var openedOn = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

        var detail = new PullRequestDetail(
            new Repository("Repository"),
            new PullRequestId(1),
            "Title",
            openedOn,
            authorId: null,
            authorDisplayName: " ",
            firstNonAuthorActivityOn: openedOn.AddHours(-1),
            lastActivityOn: openedOn.AddHours(1),
            hasCurrentUserDiscussion: false,
            descriptionText: " ",
            requestChangesCount: 0,
            hasCurrentUserRequestChanges: true,
            approvalsCount: 0,
            hasCurrentUserApproval: true);

        detail.AuthorDisplayName.Should().BeNull();
        detail.DescriptionText.Should().BeNull();
        detail.TimeToFirstResponse.Should().Be(TimeSpan.Zero);
        detail.GetOpenDuration(openedOn.AddHours(-1)).Should().Be(TimeSpan.Zero);
        detail.GetLastActivityAge(openedOn).Should().Be(TimeSpan.Zero);
        detail.HasCurrentUserRequestChanges.Should().BeFalse();
        detail.HasCurrentUserApproval.Should().BeFalse();
        detail.HasCurrentUserActivity.Should().BeFalse();
    }

    [Theory(DisplayName = "Pull request detail detects each current user activity kind")]
    [Trait("Category", "Unit")]
    [InlineData(true, 0, false, 0, false)]
    [InlineData(false, 1, true, 0, false)]
    [InlineData(false, 0, false, 1, true)]
    public void PullRequestDetailWhenCurrentUserHasActivityReturnsTrue(
        bool hasDiscussion,
        int requestChangesCount,
        bool hasRequestChanges,
        int approvalsCount,
        bool hasApproval)
    {
        var detail = new PullRequestDetail(
            new Repository("Repository"),
            new PullRequestId(1),
            "Title",
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
            authorId: null,
            authorDisplayName: null,
            firstNonAuthorActivityOn: null,
            lastActivityOn: null,
            hasDiscussion,
            requestChangesCount: requestChangesCount,
            hasCurrentUserRequestChanges: hasRequestChanges,
            approvalsCount: approvalsCount,
            hasCurrentUserApproval: hasApproval);

        detail.HasCurrentUserActivity.Should().BeTrue();
    }

    [Theory(DisplayName = "Pull request detail rejects invalid constructor values")]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void PullRequestDetailWhenValueIsInvalidThrowsArgumentException(int invalidValue)
    {
        var repository = new Repository("Repository");
        var openedOn = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

        Action act = invalidValue switch
        {
            0 => () => _ = CreateDetail(repository, openedOn, title: " "),
            1 => () => _ = CreateDetail(repository, openedOn, commentsCount: -1),
            2 => () => _ = CreateDetail(repository, openedOn, requestChangesCount: -1),
            _ => () => _ = CreateDetail(repository, openedOn, approvalsCount: -1)
        };

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Pull request detail rejects null repository")]
    [Trait("Category", "Unit")]
    public void PullRequestDetailWhenRepositoryIsNullThrowsArgumentNullException()
    {
        Action act = () => _ = CreateDetail(
            null!,
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Pull request detail rejects negative description threshold")]
    [Trait("Category", "Unit")]
    public void HasShortOrMissingDescriptionWhenThresholdIsNegativeThrowsArgumentOutOfRangeException()
    {
        var detail = CreateDetail(
            new Repository("Repository"),
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));

        Action act = () => detail.HasShortOrMissingDescription(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static PullRequestDetail CreateDetail(
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
            commentsCount: commentsCount,
            requestChangesCount: requestChangesCount,
            approvalsCount: approvalsCount);
}
