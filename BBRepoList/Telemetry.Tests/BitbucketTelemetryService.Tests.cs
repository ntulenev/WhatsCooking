using BBRepoList.Configuration;
using BBRepoList.Models;

using FluentAssertions;

using Microsoft.Extensions.Options;

namespace BBRepoList.Telemetry.Tests;

public sealed class BitbucketTelemetryServiceTests
{
    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new BitbucketTelemetryService(options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Track request throws when URI is null")]
    [Trait("Category", "Unit")]
    public void TrackRequestWhenRequestUriIsNullThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService(enabled: true);
        Uri requestUri = null!;

        // Act
        Action act = () => service.TrackRequest(requestUri);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Disabled telemetry ignores requests and returns empty snapshot")]
    [Trait("Category", "Unit")]
    public void GetSnapshotWhenTelemetryIsDisabledReturnsDisabledEmptySnapshot()
    {
        // Arrange
        var service = CreateService(enabled: false);
        service.TrackRequest(new Uri("https://api.bitbucket.org/2.0/user"));

        // Act
        var snapshot = service.GetSnapshot();

        // Assert
        snapshot.IsEnabled.Should().BeFalse();
        snapshot.TotalRequests.Should().Be(0);
        snapshot.RequestStatistics.Should().BeEmpty();
    }

    [Theory(DisplayName = "Track request normalizes known Bitbucket endpoints")]
    [Trait("Category", "Unit")]
    [InlineData("https://api.bitbucket.org/2.0/user", "GET /user")]
    [InlineData("/2.0/user", "GET /user")]
    [InlineData("https://api.bitbucket.org/2.0/repositories/workspace", "GET /repositories/{workspace}")]
    [InlineData("/2.0/repositories/workspace/repository/pullrequests", "GET /repositories/{workspace}/{repository}/pullrequests")]
    [InlineData("/2.0/repositories/workspace/repository/pullrequests?fields=size", "GET /repositories/{workspace}/{repository}/pullrequests (count)")]
    [InlineData("/2.0/repositories/workspace/repository/pullrequests?FIELDS=SIZE&pagelen=1", "GET /repositories/{workspace}/{repository}/pullrequests (count)")]
    [InlineData("/2.0/repositories/workspace/repository/pullrequests/42/activity", "GET /repositories/{workspace}/{repository}/pullrequests/{pullRequestId}/activity")]
    public void TrackRequestWhenEndpointIsKnownUsesNormalizedApiName(string requestTarget, string expectedApiName)
    {
        // Arrange
        var service = CreateService(enabled: true);

        // Act
        service.TrackRequest(CreateUri(requestTarget));
        var snapshot = service.GetSnapshot();

        // Assert
        snapshot.IsEnabled.Should().BeTrue();
        snapshot.TotalRequests.Should().Be(1);
        snapshot.RequestStatistics.Should().ContainSingle()
            .Which.Should().Be(new BitbucketApiRequestStatistic(expectedApiName, 1));
    }

    [Theory(DisplayName = "Track request normalizes root and unknown endpoints")]
    [Trait("Category", "Unit")]
    [InlineData("/", "GET /")]
    [InlineData("/2.0/", "GET /")]
    [InlineData("/2.0/workspaces/workspace/members", "GET /workspaces/workspace/members")]
    [InlineData("/repositories/workspace/repository/pullrequests/not-a-number/activity", "GET /repositories/workspace/repository/pullrequests/not-a-number/activity")]
    public void TrackRequestWhenEndpointIsOtherUsesNormalizedPath(string requestTarget, string expectedApiName)
    {
        // Arrange
        var service = CreateService(enabled: true);

        // Act
        service.TrackRequest(CreateUri(requestTarget));
        var snapshot = service.GetSnapshot();

        // Assert
        snapshot.RequestStatistics.Should().ContainSingle()
            .Which.ApiName.Should().Be(expectedApiName);
    }

    [Fact(DisplayName = "Get snapshot aggregates and sorts request statistics")]
    [Trait("Category", "Unit")]
    public void GetSnapshotWhenRequestsAreTrackedAggregatesAndSortsStatistics()
    {
        // Arrange
        var service = CreateService(enabled: true);
        service.TrackRequest(new Uri("/2.0/user", UriKind.Relative));
        service.TrackRequest(new Uri("/2.0/repositories/workspace", UriKind.Relative));
        service.TrackCacheHit();
        service.TrackCacheMiss();
        service.TrackRequest(new Uri("/2.0/repositories/workspace", UriKind.Relative));
        service.TrackRequest(new Uri("/2.0/alpha", UriKind.Relative));
        service.TrackRequest(new Uri("/2.0/alpha", UriKind.Relative));

        // Act
        var snapshot = service.GetSnapshot();

        // Assert
        snapshot.TotalRequests.Should().Be(5);
        snapshot.RequestStatistics.Should().Equal(
        [
            new BitbucketApiRequestStatistic("GET /alpha", 2),
            new BitbucketApiRequestStatistic("GET /repositories/{workspace}", 2),
            new BitbucketApiRequestStatistic("GET /user", 1)
        ]);
    }

    [Fact(DisplayName = "Reset clears tracked request statistics")]
    [Trait("Category", "Unit")]
    public void ResetWhenRequestsAreTrackedStartsNewTelemetryInterval()
    {
        // Arrange
        var service = CreateService(enabled: true);
        service.TrackRequest(new Uri("/2.0/user", UriKind.Relative));
        service.TrackRequest(new Uri("/2.0/repositories/workspace", UriKind.Relative));

        // Act
        service.Reset();
        service.TrackRequest(new Uri("/2.0/user", UriKind.Relative));
        var snapshot = service.GetSnapshot();

        // Assert
        snapshot.TotalRequests.Should().Be(1);
        snapshot.RequestStatistics.Should().ContainSingle()
            .Which.Should().Be(new BitbucketApiRequestStatistic("GET /user", 1));
        snapshot.CacheHits.Should().Be(0);
        snapshot.CacheMisses.Should().Be(0);
    }

    [Fact(DisplayName = "Cache telemetry tracks hits and misses")]
    [Trait("Category", "Unit")]
    public void TrackCacheWhenTelemetryIsEnabledCountsCacheOutcomes()
    {
        // Arrange
        var service = CreateService(enabled: true);

        // Act
        service.TrackCacheHit();
        service.TrackCacheHit();
        service.TrackCacheMiss();
        var snapshot = service.GetSnapshot();

        // Assert
        snapshot.CacheHits.Should().Be(2);
        snapshot.CacheMisses.Should().Be(1);
        snapshot.TotalRequests.Should().Be(0);
    }

    [Fact(DisplayName = "Track request is thread safe")]
    [Trait("Category", "Unit")]
    public void TrackRequestWhenCalledConcurrentlyCountsEveryRequest()
    {
        // Arrange
        var service = CreateService(enabled: true);
        var requestUri = new Uri("/2.0/user", UriKind.Relative);

        // Act
        Parallel.For(0, 1000, _ => service.TrackRequest(requestUri));
        var snapshot = service.GetSnapshot();

        // Assert
        snapshot.TotalRequests.Should().Be(1000);
        snapshot.RequestStatistics.Should().ContainSingle()
            .Which.Should().Be(new BitbucketApiRequestStatistic("GET /user", 1000));
    }

    private static BitbucketTelemetryService CreateService(bool enabled) =>
        new(Options.Create(new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/"),
            Telemetry = new BitbucketTelemetryOptions
            {
                Enabled = enabled
            }
        }));

    private static Uri CreateUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : new Uri(value, UriKind.Relative);
}
