using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class PullRequestModelsTests
{
    [Fact(DisplayName = "Pull request detail maps normalized values and calculated properties")]
    [Trait("Category", "Unit")]
    public void PullRequestDetailWhenCreatedMapsValuesAndCalculations()
    {
        // Arrange
        var openedOn = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var firstActivityOn = openedOn.AddHours(2);
        var lastActivityOn = openedOn.AddHours(5);
        var repository = new Repository("Repository", slug: new RepositorySlug("repo"));

        // Act
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

        // Assert
        detail.Title.Should().Be("Title");
        detail.DescriptionText.Should().Be("Description");
        detail.AuthorDisplayName.Should().Be("Author");
        detail.RepositoryName.Should().Be("Repository");
        detail.RepositorySlug.Should().Be(new RepositorySlug("repo"));
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
        // Arrange
        var openedOn = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

        // Act
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

        // Assert
        detail.AuthorDisplayName.Should().BeNull();
        detail.DescriptionText.Should().BeNull();
        detail.TimeToFirstResponse.Should().Be(TimeSpan.Zero);
        detail.GetOpenDuration(openedOn.AddHours(-1)).Should().Be(TimeSpan.Zero);
        detail.GetLastActivityAge(openedOn).Should().Be(TimeSpan.Zero);
        detail.HasCurrentUserRequestChanges.Should().BeFalse();
        detail.HasCurrentUserApproval.Should().BeFalse();
        detail.HasCurrentUserActivity.Should().BeFalse();
    }

    [Theory(DisplayName = "Pull request detail rejects invalid constructor values")]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void PullRequestDetailWhenValueIsInvalidThrowsArgumentException(int invalidValue)
    {
        // Arrange
        var repository = new Repository("Repository");
        var openedOn = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

        // Act
        Action act = invalidValue switch
        {
            0 => () => _ = CreateDetail(repository, openedOn, title: " "),
            1 => () => _ = CreateDetail(repository, openedOn, commentsCount: -1),
            _ => () => _ = CreateDetail(repository, openedOn, approvalsCount: -1)
        };

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Pull request detail rejects negative description threshold")]
    [Trait("Category", "Unit")]
    public void HasShortOrMissingDescriptionWhenThresholdIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var detail = CreateDetail(
            new Repository("Repository"),
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));

        // Act
        Action act = () => detail.HasShortOrMissingDescription(-1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Merged pull request maps merge calculations")]
    [Trait("Category", "Unit")]
    public void MergedPullRequestWhenCreatedMapsMergeCalculations()
    {
        // Arrange
        var openedOn = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var mergedOn = openedOn.AddDays(2);

        // Act
        var pullRequest = new MergedPullRequest(
            new Repository("Repository", slug: new RepositorySlug("repo")),
            new PullRequestId(7),
            "Title",
            openedOn,
            authorId: null,
            authorDisplayName: null,
            firstNonAuthorActivityOn: openedOn.AddHours(1),
            lastActivityOn: mergedOn.AddHours(-1),
            hasCurrentUserDiscussion: false,
            mergedOn,
            approvalsCount: 1,
            hasCurrentUserApproval: true);

        // Assert
        pullRequest.GetOpenDuration().Should().Be(TimeSpan.FromDays(2));
        pullRequest.GetLastActivityAge(mergedOn).Should().Be(TimeSpan.FromHours(1));
        pullRequest.TimeToFirstResponse.Should().Be(TimeSpan.FromHours(1));
        pullRequest.HasCurrentUserActivity.Should().BeTrue();
    }

    [Fact(DisplayName = "Merged pull request clamps negative open duration")]
    [Trait("Category", "Unit")]
    public void MergedPullRequestWhenMergedBeforeOpeningClampsDuration()
    {
        // Arrange
        var openedOn = new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero);

        // Act
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

        // Assert
        pullRequest.GetOpenDuration().Should().Be(TimeSpan.Zero);
        pullRequest.GetLastActivityAge(openedOn).Should().BeNull();
    }

    [Fact(DisplayName = "Simple pull request records preserve values")]
    [Trait("Category", "Unit")]
    public void PullRequestRecordsWhenCreatedPreserveValues()
    {
        // Arrange
        var id = new PullRequestId(5);
        var happenedOn = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        var activity = new PullRequestActivityEntry(new BitbucketId("actor"), happenedOn, true);
        var summary = new PullRequestActivitySummary(happenedOn, happenedOn.AddHours(1), true, 2);
        var cacheEntry = new PullRequestDetailsCacheEntry(id, "fingerprint", happenedOn, happenedOn.AddHours(1), true, 2);
        var snapshot = new PullRequestSnapshot(id, "Title", happenedOn, CacheFingerprint: "fingerprint");

        // Assert
        activity.Should().Be(new PullRequestActivityEntry(new BitbucketId("{ACTOR}"), happenedOn, true));
        summary.CommentsCount.Should().Be(2);
        cacheEntry.PullRequestId.Should().Be(id);
        snapshot.CacheFingerprint.Should().Be("fingerprint");
    }

    [Fact(DisplayName = "Telemetry and user models preserve values")]
    [Trait("Category", "Unit")]
    public void TelemetryAndUserModelsWhenCreatedPreserveValues()
    {
        // Arrange
        var statistic = new BitbucketApiRequestStatistic("repositories", 3);
        var user = new BitbucketUser(new BitbucketId("user"), new UserName("User"));

        // Act
        var snapshot = new BitbucketTelemetrySnapshot(true, 3, [statistic]);

        // Assert
        snapshot.RequestStatistics.Should().ContainSingle().Which.Should().Be(statistic);
        user.Uuid.Should().Be(new BitbucketId("{USER}"));
        user.DisplayName.Should().Be(new UserName("User"));
    }

    private static PullRequestDetail CreateDetail(
        Repository repository,
        DateTimeOffset openedOn,
        string title = "Title",
        int commentsCount = 0,
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
            approvalsCount: approvalsCount);
}
