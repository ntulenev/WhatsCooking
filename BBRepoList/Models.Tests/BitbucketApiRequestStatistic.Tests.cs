using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class BitbucketApiRequestStatisticTests
{
    [Fact(DisplayName = "Bitbucket API request statistic preserves values")]
    [Trait("Category", "Unit")]
    public void BitbucketApiRequestStatisticWhenCreatedPreservesValues()
    {
        var statistic = new BitbucketApiRequestStatistic("repositories", 3);

        statistic.ApiName.Should().Be("repositories");
        statistic.RequestCount.Should().Be(3);
    }
}
