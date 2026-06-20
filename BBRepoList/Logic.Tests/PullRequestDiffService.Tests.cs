using BBRepoList.Models;

using FluentAssertions;

namespace BBRepoList.Logic.Tests;

public sealed class PullRequestDiffServiceTests
{
    [Fact(DisplayName = "Compare counts current pull requests missing from previous load")]
    [Trait("Category", "Unit")]
    public void CompareWhenCurrentLoadContainsNewPullRequestsReturnsNewCounts()
    {
        // Arrange
        var repository = new Repository("Payments", slug: new RepositorySlug("payments"));
        var service = new PullRequestDiffService();

        // Act
        var result = service.Compare(
            previousOpenPullRequests: [CreateOpenPullRequest(repository, 1)],
            previousMergedPullRequests: [CreateMergedPullRequest(repository, 10)],
            currentOpenPullRequests:
            [
                CreateOpenPullRequest(repository, 1),
                CreateOpenPullRequest(repository, 2),
                CreateOpenPullRequest(repository, 3)
            ],
            currentMergedPullRequests:
            [
                CreateMergedPullRequest(repository, 10),
                CreateMergedPullRequest(repository, 11)
            ]);

        // Assert
        result.Should().Be(new PullRequestDiffSummary(2, 1));
        result.HasNewPullRequests.Should().BeTrue();
    }

    [Fact(DisplayName = "Compare uses repository slug as stable pull request identity")]
    [Trait("Category", "Unit")]
    public void CompareWhenRepositoryNameChangesButSlugMatchesDoesNotReportNewPullRequest()
    {
        // Arrange
        var previousRepository = new Repository("Old Payments", slug: new RepositorySlug("payments"));
        var currentRepository = new Repository("New Payments", slug: new RepositorySlug("payments"));
        var service = new PullRequestDiffService();

        // Act
        var result = service.Compare(
            previousOpenPullRequests: [CreateOpenPullRequest(previousRepository, 1)],
            previousMergedPullRequests: [CreateMergedPullRequest(previousRepository, 10)],
            currentOpenPullRequests: [CreateOpenPullRequest(currentRepository, 1)],
            currentMergedPullRequests: [CreateMergedPullRequest(currentRepository, 10)]);

        // Assert
        result.Should().Be(new PullRequestDiffSummary(0, 0));
        result.HasNewPullRequests.Should().BeFalse();
    }

    [Fact(DisplayName = "Compare throws when required collection is null")]
    [Trait("Category", "Unit")]
    public void CompareWhenRequiredCollectionIsNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new PullRequestDiffService();

        // Act
        Action act = () => _ = service.Compare(
            previousOpenPullRequests: null!,
            previousMergedPullRequests: [],
            currentOpenPullRequests: [],
            currentMergedPullRequests: []);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private static PullRequestDetail CreateOpenPullRequest(Repository repository, int id) =>
        new(
            repository,
            new PullRequestId(id),
            $"Open PR {id}",
            _openedOn,
            authorId: null,
            authorDisplayName: null,
            firstNonAuthorActivityOn: null,
            lastActivityOn: null,
            hasCurrentUserDiscussion: false);

    private static MergedPullRequest CreateMergedPullRequest(Repository repository, int id) =>
        new(
            repository,
            new PullRequestId(id),
            $"Merged PR {id}",
            _openedOn,
            authorId: null,
            authorDisplayName: null,
            firstNonAuthorActivityOn: null,
            lastActivityOn: null,
            hasCurrentUserDiscussion: false,
            mergedOn: _openedOn.AddDays(1));

    private static readonly DateTimeOffset _openedOn = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
}
