using FluentAssertions;

using WhatsCooking.Services;

namespace WhatsCooking.Presentation.Tests;

public sealed class DemoProvidersTests
{
    [Fact(DisplayName = "Demo pull request provider constructor throws when time provider is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTimeProviderIsNullThrowsArgumentNullException()
    {
        // Arrange
        TimeProvider timeProvider = null!;

        // Act
        Action act = () => _ = new DemoPullRequestDashboardProvider(timeProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Demo pull request provider creates consistent dashboard data")]
    [Trait("Category", "Unit")]
    public void CreateWhenCalledReturnsConsistentDashboardData()
    {
        // Arrange
        var asOf = new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
        var provider = new DemoPullRequestDashboardProvider(new FixedTimeProvider(asOf));

        // Act
        var result = provider.Create();

        // Assert
        result.Repositories.Should().HaveCount(10);
        result.OpenPullRequests.Should().HaveCount(10);
        result.MergedPullRequests.Should().HaveCount(10);
        result.Repositories.Should().OnlyContain(repository => repository.Slug != null);
        result.OpenPullRequests.Should().OnlyContain(pullRequest =>
            pullRequest.OpenedOn < asOf
            && result.Repositories.Contains(pullRequest.Repository));
        result.MergedPullRequests.Should().OnlyContain(pullRequest =>
            pullRequest.MergedOn < asOf
            && result.Repositories.Contains(pullRequest.Repository));
    }

    [Fact(DisplayName = "Demo telemetry provider creates internally consistent snapshot")]
    [Trait("Category", "Unit")]
    public void CreateWhenCalledReturnsConsistentTelemetrySnapshot()
    {
        // Arrange
        var provider = new DemoTelemetryProvider();

        // Act
        var result = provider.Create();

        // Assert
        result.IsEnabled.Should().BeTrue();
        result.RequestStatistics.Should().HaveCount(10);
        result.TotalRequests.Should().Be(result.RequestStatistics.Sum(statistic => statistic.RequestCount));
        result.RequestStatistics.Should().OnlyContain(statistic => statistic.RequestCount > 0);
        result.CacheHits.Should().BeGreaterThan(0);
        result.CacheMisses.Should().BeGreaterThan(0);
    }
}
