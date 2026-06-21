using BBRepoList.Models;

using FluentAssertions;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Presentation.Tests;

public sealed class PullRequestRowTests
{
    [Fact(DisplayName = "Open row constructor throws when detail is null")]
    [Trait("Category", "Unit")]
    public void OpenRowConstructorWhenDetailIsNullThrowsArgumentNullException()
    {
        // Arrange
        PullRequestDetail detail = null!;

        // Act
        Action act = () => _ = new PullRequestRow(1, detail, DateTimeOffset.UtcNow, CreateOptions());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Open row constructor maps formatting and activity priority")]
    [Trait("Category", "Unit")]
    public void OpenRowConstructorWhenDetailIsValidMapsFormattingAndActivityPriority()
    {
        // Arrange
        var asOf = new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
        var detail = new PullRequestDetail(
            new Repository("Payments API", slug: new RepositorySlug("payments api")),
            new PullRequestId(42),
            "Add retries",
            asOf.AddDays(-1).AddHours(-2).AddMinutes(-3),
            null,
            null,
            asOf.AddHours(-25),
            asOf.AddMinutes(-45),
            true,
            "short",
            commentsCount: 5,
            requestChangesCount: 1,
            hasCurrentUserRequestChanges: true,
            approvalsCount: 2,
            hasCurrentUserApproval: true);

        // Act
        var row = new PullRequestRow(7, detail, asOf, CreateOptions());

        // Assert
        row.Should().BeEquivalentTo(new
        {
            Number = 7,
            RepositoryName = "Payments API",
            PullRequestId = 42,
            PullRequestDisplay = "#42 Add retries",
            Author = "-",
            DescriptionLength = 5,
            IsDescriptionShort = true,
            OpenFor = "1d 2h 3m",
            OpenForMinutes = 1563d,
            TimeToFirstResponse = "1h 3m",
            TimeToFirstResponseMinutes = 63d,
            ActivityAgeOrMerged = "45m",
            ActivityAgeOrMergedMinutes = 45d,
            CommentsCount = 5,
            HasRequestChanges = true,
            RequestChanges = "RC (1)",
            HasApprovals = true,
            Approvals = "AP (2)",
            HasCurrentUserActivity = true,
            CurrentUserActivity = "Request changes"
        });
        row.RepositoryUrl.Should().Be("https://bitbucket.org/platform/payments%20api");
        row.PullRequestUrl.Should().Be("https://bitbucket.org/platform/payments%20api/pull-requests/42");
        row.SearchText.Should().ContainAll("Payments API", "Add retries", "Request changes");
    }

    [Fact(DisplayName = "Open row marks missing overdue response as alert")]
    [Trait("Category", "Unit")]
    public void OpenRowConstructorWhenResponseIsMissingAndOverdueMarksAlert()
    {
        // Arrange
        var asOf = new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
        var detail = new PullRequestDetail(
            new Repository("Repo", slug: new RepositorySlug("repo")),
            new PullRequestId(1),
            "Title",
            asOf.AddHours(-5),
            null,
            "Author",
            null,
            null,
            false);

        // Act
        var row = new PullRequestRow(1, detail, asOf, CreateOptions());

        // Assert
        row.IsTtfrAlert.Should().BeTrue();
        row.TimeToFirstResponse.Should().Be("ALERT");
        row.TimeToFirstResponseMinutes.Should().Be(-1);
        row.ActivityAgeOrMerged.Should().Be("-");
    }

    [Fact(DisplayName = "Open row shows current user approval when no request changes exist")]
    [Trait("Category", "Unit")]
    public void OpenRowConstructorWhenCurrentUserApprovedShowsApprovalActivity()
    {
        // Arrange
        var asOf = new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
        var detail = new PullRequestDetail(
            new Repository("Repo", slug: new RepositorySlug("repo")),
            new PullRequestId(1),
            "Title",
            asOf.AddHours(-1),
            null,
            "Author",
            firstNonAuthorActivityOn: null,
            lastActivityOn: null,
            hasCurrentUserDiscussion: false,
            approvalsCount: 1,
            hasCurrentUserApproval: true);

        // Act
        var row = new PullRequestRow(1, detail, asOf, CreateOptions());

        // Assert
        row.HasCurrentUserApproval.Should().BeTrue();
        row.HasCurrentUserActivity.Should().BeTrue();
        row.CurrentUserActivity.Should().Be("Approval");
    }

    [Fact(DisplayName = "Open row shows current user comment when comment is only activity")]
    [Trait("Category", "Unit")]
    public void OpenRowConstructorWhenCurrentUserCommentedShowsCommentActivity()
    {
        // Arrange
        var asOf = new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
        var detail = new PullRequestDetail(
            new Repository("Repo", slug: new RepositorySlug("repo")),
            new PullRequestId(1),
            "Title",
            asOf.AddHours(-1),
            null,
            "Author",
            firstNonAuthorActivityOn: null,
            lastActivityOn: asOf.AddMinutes(-5),
            hasCurrentUserDiscussion: true);

        // Act
        var row = new PullRequestRow(1, detail, asOf, CreateOptions());

        // Assert
        row.HasCurrentUserDiscussion.Should().BeTrue();
        row.HasCurrentUserActivity.Should().BeTrue();
        row.CurrentUserActivity.Should().Be("Comment");
    }

    [Fact(DisplayName = "Merged row constructor maps merge age and review state")]
    [Trait("Category", "Unit")]
    public void MergedRowConstructorWhenPullRequestIsValidMapsMergeAgeAndReviewState()
    {
        // Arrange
        var asOf = new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
        var pullRequest = new MergedPullRequest(
            new Repository("Repo", slug: new RepositorySlug("repo")),
            new PullRequestId(5),
            "Merged title",
            asOf.AddDays(-3),
            null,
            "Nikita",
            null,
            null,
            false,
            asOf.AddDays(-1));
        var propertyChanges = new List<string?>();
        var row = new PullRequestRow(1, pullRequest, asOf, CreateOptions());
        row.PropertyChanged += (_, args) => propertyChanges.Add(args.PropertyName);

        // Act
        row.IsReviewed = true;
        row.IsReviewed = true;

        // Assert
        row.OpenFor.Should().Be("2d 0h 0m");
        row.ActivityAgeOrMerged.Should().Be("1d 0h 0m");
        row.IsTtfrAlert.Should().BeFalse();
        row.IsReviewed.Should().BeTrue();
        propertyChanges.Should().Equal(nameof(PullRequestRow.IsReviewed));
    }

    private static PullRequestPresentationOptions CreateOptions() =>
        new(new BitbucketWorkspace("platform"), 10, TimeSpan.FromHours(4));
}
