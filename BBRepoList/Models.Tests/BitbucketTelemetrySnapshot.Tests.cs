using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class BitbucketTelemetrySnapshotTests
{
    [Fact(DisplayName = "Bitbucket telemetry snapshot preserves values")]
    [Trait("Category", "Unit")]
    public void BitbucketTelemetrySnapshotWhenCreatedPreservesValues()
    {
        var statistic = new BitbucketApiRequestStatistic("repositories", 3);

        var snapshot = new BitbucketTelemetrySnapshot(true, 3, [statistic], CacheHits: 4, CacheMisses: 2);

        snapshot.IsEnabled.Should().BeTrue();
        snapshot.TotalRequests.Should().Be(3);
        snapshot.RequestStatistics.Should().ContainSingle().Which.Should().Be(statistic);
        snapshot.CacheHits.Should().Be(4);
        snapshot.CacheMisses.Should().Be(2);
    }
}
