using BBRepoList.Models;

using FluentAssertions;

namespace BBRepoList.Logic.Tests;

public sealed class PullRequestResultSorterTests
{
    [Fact(DisplayName = "Sort open pull requests orders newest first then repository then id")]
    [Trait("Category", "Unit")]
    public void SortOpenWhenPullRequestsAreUnorderedReturnsNewestThenRepositoryThenId()
    {
        // Arrange
        var service = new PullRequestResultSorter();
        var newest = CreateOpenPullRequest(
            "zeta",
            3,
            new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));
        var sameDateZeta = CreateOpenPullRequest(
            "zeta",
            2,
            new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));
        var sameDateAlphaHighId = CreateOpenPullRequest(
            "Alpha",
            4,
            sameDateZeta.OpenedOn);
        var sameDateAlphaLowId = CreateOpenPullRequest(
            "Alpha",
            1,
            sameDateZeta.OpenedOn);

        // Act
        var result = service.SortOpen(
            [sameDateZeta, sameDateAlphaHighId, newest, sameDateAlphaLowId]);

        // Assert
        result.Should().Equal(newest, sameDateAlphaLowId, sameDateAlphaHighId, sameDateZeta);
    }

    [Fact(DisplayName = "Sort open pull requests throws when input is null")]
    [Trait("Category", "Unit")]
    public void SortOpenWhenPullRequestsAreNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new PullRequestResultSorter();
        IReadOnlyList<PullRequestDetail> pullRequests = null!;

        // Act
        Action act = () => _ = service.SortOpen(pullRequests);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Sort merged pull requests orders newest first then repository then id")]
    [Trait("Category", "Unit")]
    public void SortMergedWhenPullRequestsAreUnorderedReturnsNewestThenRepositoryThenId()
    {
        // Arrange
        var service = new PullRequestResultSorter();
        var newest = CreateMergedPullRequest(
            "zeta",
            3,
            new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));
        var sameDateZeta = CreateMergedPullRequest(
            "zeta",
            2,
            new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));
        var sameDateAlphaHighId = CreateMergedPullRequest(
            "Alpha",
            4,
            sameDateZeta.MergedOn);
        var sameDateAlphaLowId = CreateMergedPullRequest(
            "Alpha",
            1,
            sameDateZeta.MergedOn);

        // Act
        var result = service.SortMerged(
            [sameDateZeta, sameDateAlphaHighId, newest, sameDateAlphaLowId]);

        // Assert
        result.Should().Equal(newest, sameDateAlphaLowId, sameDateAlphaHighId, sameDateZeta);
    }

    [Fact(DisplayName = "Sort merged pull requests throws when input is null")]
    [Trait("Category", "Unit")]
    public void SortMergedWhenPullRequestsAreNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new PullRequestResultSorter();
        IReadOnlyList<MergedPullRequest> pullRequests = null!;

        // Act
        Action act = () => _ = service.SortMerged(pullRequests);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private static PullRequestDetail CreateOpenPullRequest(
        string repositoryName,
        int id,
        DateTimeOffset openedOn) =>
        new(
            new Repository(repositoryName, slug: new RepositorySlug(repositoryName)),
            new PullRequestId(id),
            $"Open PR {id}",
            openedOn,
            authorId: null,
            authorDisplayName: null,
            firstNonAuthorActivityOn: null,
            lastActivityOn: null,
            hasCurrentUserDiscussion: false);

    private static MergedPullRequest CreateMergedPullRequest(
        string repositoryName,
        int id,
        DateTimeOffset mergedOn) =>
        new(
            new Repository(repositoryName, slug: new RepositorySlug(repositoryName)),
            new PullRequestId(id),
            $"Merged PR {id}",
            mergedOn.AddDays(-1),
            authorId: null,
            authorDisplayName: null,
            firstNonAuthorActivityOn: null,
            lastActivityOn: null,
            hasCurrentUserDiscussion: false,
            mergedOn);
}
