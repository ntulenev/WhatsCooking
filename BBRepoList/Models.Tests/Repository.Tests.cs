using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class RepositoryTests
{
    [Fact(DisplayName = "Repository trims name and exposes available capabilities")]
    [Trait("Category", "Unit")]
    public void RepositoryWhenCreatedMapsValuesAndCapabilities()
    {
        var createdOn = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedOn = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var slug = new RepositorySlug("repo");

        var repository = new Repository(" Repository ", createdOn, updatedOn, slug);

        repository.Name.Should().Be("Repository");
        repository.CreatedOn.Should().Be(createdOn);
        repository.LastUpdatedOn.Should().Be(updatedOn);
        repository.Slug.Should().Be(slug);
        repository.OpenPullRequestsCount.Should().Be(0);
        repository.CanPopulateOpenPullRequestsCount.Should().BeTrue();
        repository.CanLoadPullRequests.Should().BeTrue();
        repository.CanCalculateInactivityTiming.Should().BeTrue();
    }

    [Fact(DisplayName = "Repository rejects empty name")]
    [Trait("Category", "Unit")]
    public void RepositoryWhenNameIsEmptyThrowsArgumentException()
    {
        Action act = () => _ = new Repository(" ");

        act.Should().Throw<ArgumentException>();
    }

    [Theory(DisplayName = "Repository calculates full inactive months")]
    [Trait("Category", "Unit")]
    [InlineData(2026, 3, 14, 1)]
    [InlineData(2026, 3, 15, 2)]
    [InlineData(2026, 1, 15, 0)]
    [InlineData(2025, 12, 15, 0)]
    public void CalculateMonthsWithoutActivityWhenDatesAreAvailableReturnsFullMonths(
        int year,
        int month,
        int day,
        int expected)
    {
        var repository = new Repository(
            "Repository",
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));

        var result = repository.CalculateMonthsWithoutActivity(
            new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero));

        result.Should().Be(expected);
    }

    [Fact(DisplayName = "Repository without timestamps returns zero inactive months")]
    [Trait("Category", "Unit")]
    public void CalculateMonthsWithoutActivityWhenTimestampsAreMissingReturnsZero()
    {
        var repository = new Repository("Repository");

        var result = repository.CalculateMonthsWithoutActivity(
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

        result.Should().Be(0);
        repository.CanCalculateInactivityTiming.Should().BeFalse();
    }

    [Fact(DisplayName = "Repository updates non-negative pull request count")]
    [Trait("Category", "Unit")]
    public void UpdateOpenPullRequestCountWhenValueIsValidUpdatesCount()
    {
        var repository = new Repository("Repository");

        repository.UpdateOpenPullRequestsCount(3);

        repository.OpenPullRequestsCount.Should().Be(3);
    }

    [Fact(DisplayName = "Repository rejects negative pull request count")]
    [Trait("Category", "Unit")]
    public void UpdateOpenPullRequestCountWhenValueIsNegativeThrowsArgumentOutOfRangeException()
    {
        var repository = new Repository("Repository");

        Action act = () => repository.UpdateOpenPullRequestsCount(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
