using BBRepoList.Models;

using FluentAssertions;

namespace BBRepoList.API.Tests;

public sealed class PullRequestActivityAnalyzerTests
{
    [Fact(DisplayName = "Create summary throws when activities are null")]
    [Trait("Category", "Unit")]
    public void CreateSummaryWhenActivitiesAreNullThrowsArgumentNullException()
    {
        // Arrange
        var analyzer = new PullRequestActivityAnalyzer();
        IReadOnlyList<PullRequestActivityEntry> activities = null!;

        // Act
        Action act = () => analyzer.CreateSummary(
            activities,
            new PullRequestSnapshot(new PullRequestId(1), "Title", DateTimeOffset.UtcNow),
            new BitbucketId("current-user"));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Create summary aggregates activity independently of input order")]
    [Trait("Category", "Unit")]
    public void CreateSummaryWhenActivitiesAreUnorderedAggregatesExpectedValues()
    {
        // Arrange
        var analyzer = new PullRequestActivityAnalyzer();
        var authorId = new BitbucketId("author");
        var currentUserId = new BitbucketId("{CURRENT-USER}");
        var first = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
        var second = first.AddHours(1);
        var last = first.AddHours(2);
        PullRequestActivityEntry[] activities =
        [
            new(new BitbucketId("other"), last, false),
            new(authorId, first, true),
            new(new BitbucketId("current-user"), second, true)
        ];
        var pullRequest = new PullRequestSnapshot(
            new PullRequestId(1),
            "Title",
            first.AddDays(-1),
            AuthorId: authorId);

        // Act
        var result = analyzer.CreateSummary(activities, pullRequest, currentUserId);

        // Assert
        result.FirstNonAuthorActivityOn.Should().Be(second);
        result.LastActivityOn.Should().Be(last);
        result.HasCurrentUserDiscussion.Should().BeTrue();
        result.CommentsCount.Should().Be(2);
    }

    [Fact(DisplayName = "Create summary returns empty values when activities are empty")]
    [Trait("Category", "Unit")]
    public void CreateSummaryWhenActivitiesAreEmptyReturnsEmptyValues()
    {
        // Arrange
        var analyzer = new PullRequestActivityAnalyzer();

        // Act
        var result = analyzer.CreateSummary(
            [],
            new PullRequestSnapshot(new PullRequestId(1), "Title", DateTimeOffset.UtcNow),
            new BitbucketId("current-user"));

        // Assert
        result.Should().BeEquivalentTo(new PullRequestActivitySummary(null, null, false, 0));
    }
}
