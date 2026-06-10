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
        // Act
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

        // Assert
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
        // Arrange
        var repository = new Repository("Repository");
        var openedOn = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

        // Act
        Action act = invalidValue switch
        {
            0 => () => _ = CreateDetail(repository, openedOn, title: " "),
            1 => () => _ = CreateDetail(repository, openedOn, commentsCount: -1),
            2 => () => _ = CreateDetail(repository, openedOn, requestChangesCount: -1),
            _ => () => _ = CreateDetail(repository, openedOn, approvalsCount: -1)
        };

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Pull request detail rejects null repository")]
    [Trait("Category", "Unit")]
    public void PullRequestDetailWhenRepositoryIsNullThrowsArgumentNullException()
    {
        // Act
        Action act = () => _ = CreateDetail(
            null!,
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));

        // Assert
        act.Should().Throw<ArgumentNullException>();
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

        // Assert
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
        // Act
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

        // Assert
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
        // Arrange
        var repository = new Repository("Repository");
        var openedOn = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

        // Act
        Action act = invalidValue switch
        {
            0 => () => _ = CreateMergedPullRequest(repository, openedOn, title: " "),
            1 => () => _ = CreateMergedPullRequest(repository, openedOn, commentsCount: -1),
            2 => () => _ = CreateMergedPullRequest(repository, openedOn, requestChangesCount: -1),
            _ => () => _ = CreateMergedPullRequest(repository, openedOn, approvalsCount: -1)
        };

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Merged pull request rejects null repository")]
    [Trait("Category", "Unit")]
    public void MergedPullRequestWhenRepositoryIsNullThrowsArgumentNullException()
    {
        // Act
        Action act = () => _ = CreateMergedPullRequest(
            null!,
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Merged pull request rejects negative description threshold")]
    [Trait("Category", "Unit")]
    public void MergedHasShortOrMissingDescriptionWhenThresholdIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var pullRequest = CreateMergedPullRequest(
            new Repository("Repository"),
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));

        // Act
        Action act = () => pullRequest.HasShortOrMissingDescription(-1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
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
        activity.ActorId.Should().Be(new BitbucketId("{ACTOR}"));
        activity.HappenedOn.Should().Be(happenedOn);
        activity.IsComment.Should().BeTrue();
        summary.FirstNonAuthorActivityOn.Should().Be(happenedOn);
        summary.LastActivityOn.Should().Be(happenedOn.AddHours(1));
        summary.HasCurrentUserDiscussion.Should().BeTrue();
        summary.CommentsCount.Should().Be(2);
        cacheEntry.PullRequestId.Should().Be(id);
        cacheEntry.Fingerprint.Should().Be("fingerprint");
        cacheEntry.FirstNonAuthorActivityOn.Should().Be(happenedOn);
        cacheEntry.LastActivityOn.Should().Be(happenedOn.AddHours(1));
        cacheEntry.HasCurrentUserDiscussion.Should().BeTrue();
        cacheEntry.CommentsCount.Should().Be(2);
        snapshot.Id.Should().Be(id);
        snapshot.Title.Should().Be("Title");
        snapshot.CreatedOn.Should().Be(happenedOn);
        snapshot.DescriptionText.Should().BeNull();
        snapshot.AuthorId.Should().BeNull();
        snapshot.AuthorDisplayName.Should().BeNull();
        snapshot.RequestChangesCount.Should().Be(0);
        snapshot.HasCurrentUserRequestChanges.Should().BeFalse();
        snapshot.ApprovalsCount.Should().Be(0);
        snapshot.HasCurrentUserApproval.Should().BeFalse();
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
        statistic.ApiName.Should().Be("repositories");
        statistic.RequestCount.Should().Be(3);
        snapshot.IsEnabled.Should().BeTrue();
        snapshot.TotalRequests.Should().Be(3);
        snapshot.RequestStatistics.Should().ContainSingle().Which.Should().Be(statistic);
        user.Uuid.Should().Be(new BitbucketId("{USER}"));
        user.DisplayName.Should().Be(new UserName("User"));
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

    private static MergedPullRequest CreateMergedPullRequest(
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
