using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class PullRequestRepositoryLoadProgressTests
{
    [Theory(DisplayName = "Pull request repository progress rejects invalid counters")]
    [Trait("Category", "Unit")]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(2, 1)]
    public void PullRequestRepositoryLoadProgressWhenCountersAreInvalidThrowsArgumentOutOfRangeException(
        int loaded,
        int total)
    {
        Action act = () => _ = new PullRequestRepositoryLoadProgress(loaded, total);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Pull request repository progress maps valid counters")]
    [Trait("Category", "Unit")]
    public void PullRequestRepositoryLoadProgressWhenCountersAreValidMapsValues()
    {
        var progress = new PullRequestRepositoryLoadProgress(2, 3);

        progress.LoadedRepositories.Should().Be(2);
        progress.TotalRepositories.Should().Be(3);
    }
}
