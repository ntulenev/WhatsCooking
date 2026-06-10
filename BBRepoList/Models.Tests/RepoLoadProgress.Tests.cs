using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class RepoLoadProgressTests
{
    [Theory(DisplayName = "Repository load progress rejects invalid counters")]
    [Trait("Category", "Unit")]
    [InlineData(-1, 0, 0, 0)]
    [InlineData(0, -1, 0, 0)]
    [InlineData(0, 0, -1, 0)]
    [InlineData(0, 0, 0, -1)]
    [InlineData(0, 0, 1, 0)]
    public void RepoLoadProgressWhenCountersAreInvalidThrowsArgumentOutOfRangeException(
        int seen,
        int matched,
        int loaded,
        int total)
    {
        Action act = () => _ = new RepoLoadProgress(
            seen,
            matched,
            pullRequestStatisticsLoaded: loaded,
            pullRequestStatisticsTotal: total);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Repository load progress maps valid counters")]
    [Trait("Category", "Unit")]
    public void RepoLoadProgressWhenCountersAreValidMapsValues()
    {
        var progress = new RepoLoadProgress(
            seen: 5,
            matched: 3,
            isLoadingPullRequestStatistics: true,
            pullRequestStatisticsLoaded: 2,
            pullRequestStatisticsTotal: 3);

        progress.Seen.Should().Be(5);
        progress.Matched.Should().Be(3);
        progress.IsLoadingPullRequestStatistics.Should().BeTrue();
        progress.PullRequestStatisticsLoaded.Should().Be(2);
        progress.PullRequestStatisticsTotal.Should().Be(3);
    }
}
