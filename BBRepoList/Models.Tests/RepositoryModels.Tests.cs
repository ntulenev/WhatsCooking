using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class RepositoryModelsTests
{
    [Fact(DisplayName = "Repository trims name and exposes available capabilities")]
    [Trait("Category", "Unit")]
    public void RepositoryWhenCreatedMapsValuesAndCapabilities()
    {
        // Arrange
        var createdOn = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedOn = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var slug = new RepositorySlug("repo");

        // Act
        var repository = new Repository(" Repository ", createdOn, updatedOn, slug);

        // Assert
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
        // Act
        Action act = () => _ = new Repository(" ");

        // Assert
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
        // Arrange
        var repository = new Repository(
            "Repository",
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));

        // Act
        var result = repository.CalculateMonthsWithoutActivity(
            new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero));

        // Assert
        result.Should().Be(expected);
    }

    [Fact(DisplayName = "Repository without timestamps returns zero inactive months")]
    [Trait("Category", "Unit")]
    public void CalculateMonthsWithoutActivityWhenTimestampsAreMissingReturnsZero()
    {
        // Arrange
        var repository = new Repository("Repository");

        // Act
        var result = repository.CalculateMonthsWithoutActivity(
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

        // Assert
        result.Should().Be(0);
        repository.CanCalculateInactivityTiming.Should().BeFalse();
    }

    [Fact(DisplayName = "Repository updates non-negative pull request count")]
    [Trait("Category", "Unit")]
    public void UpdateOpenPullRequestCountWhenValueIsValidUpdatesCount()
    {
        // Arrange
        var repository = new Repository("Repository");

        // Act
        repository.UpdateOpenPullRequestsCount(3);

        // Assert
        repository.OpenPullRequestsCount.Should().Be(3);
    }

    [Fact(DisplayName = "Repository rejects negative pull request count")]
    [Trait("Category", "Unit")]
    public void UpdateOpenPullRequestCountWhenValueIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var repository = new Repository("Repository");

        // Act
        Action act = () => repository.UpdateOpenPullRequestsCount(-1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory(DisplayName = "Filter pattern matches repository names by configured mode")]
    [Trait("Category", "Unit")]
    [InlineData("api", RepositorySearchMode.Contains, true)]
    [InlineData("PAY", RepositorySearchMode.StartWith, true)]
    [InlineData("api", RepositorySearchMode.StartWith, false)]
    [InlineData(null, RepositorySearchMode.Contains, true)]
    [InlineData(" ", RepositorySearchMode.Contains, true)]
    public void FilterWhenAppliedReturnsExpectedResult(
        string? phrase,
        RepositorySearchMode searchMode,
        bool expected)
    {
        // Arrange
        var filter = new FilterPattern(phrase, searchMode);
        var repository = new Repository("Payments API");

        // Act
        var result = filter.Filter(repository);

        // Assert
        result.Should().Be(expected);
        filter.HasFilter.Should().Be(!string.IsNullOrWhiteSpace(phrase));
    }

    [Fact(DisplayName = "Filter pattern rejects null repository")]
    [Trait("Category", "Unit")]
    public void FilterWhenRepositoryIsNullThrowsArgumentNullException()
    {
        // Arrange
        var filter = new FilterPattern("api");
        Repository repository = null!;

        // Act
        Action act = () => filter.Filter(repository);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Filter pattern rejects unsupported search mode")]
    [Trait("Category", "Unit")]
    public void FilterWhenSearchModeIsUnsupportedThrowsInvalidOperationException()
    {
        // Arrange
        var filter = new FilterPattern("api", (RepositorySearchMode)42);

        // Act
        Action act = () => filter.Filter(new Repository("API"));

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact(DisplayName = "Repository page preserves values and next link")]
    [Trait("Category", "Unit")]
    public void RepoPageWhenValuesAreValidMapsValues()
    {
        // Arrange
        Repository[] repositories = [new("One"), new("Two")];
        var next = new Uri("https://api.bitbucket.org/next");

        // Act
        var page = new RepoPage(repositories, next);

        // Assert
        page.Values.Should().BeSameAs(repositories);
        page.Next.Should().Be(next);
    }

    [Fact(DisplayName = "Repository page rejects null collection and entries")]
    [Trait("Category", "Unit")]
    public void RepoPageWhenValuesAreInvalidThrowsArgumentException()
    {
        // Act
        Action nullCollection = () => _ = new RepoPage(null!, null);
        Action nullEntry = () => _ = new RepoPage([null!], null);

        // Assert
        nullCollection.Should().Throw<ArgumentNullException>();
        nullEntry.Should().Throw<ArgumentException>();
    }

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
        // Act
        Action act = () => _ = new RepoLoadProgress(
            seen,
            matched,
            pullRequestStatisticsLoaded: loaded,
            pullRequestStatisticsTotal: total);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Repository load progress maps valid counters")]
    [Trait("Category", "Unit")]
    public void RepoLoadProgressWhenCountersAreValidMapsValues()
    {
        // Act
        var progress = new RepoLoadProgress(
            seen: 5,
            matched: 3,
            isLoadingPullRequestStatistics: true,
            pullRequestStatisticsLoaded: 2,
            pullRequestStatisticsTotal: 3);

        // Assert
        progress.Seen.Should().Be(5);
        progress.Matched.Should().Be(3);
        progress.IsLoadingPullRequestStatistics.Should().BeTrue();
        progress.PullRequestStatisticsLoaded.Should().Be(2);
        progress.PullRequestStatisticsTotal.Should().Be(3);
    }

    [Theory(DisplayName = "Pull request repository progress rejects invalid counters")]
    [Trait("Category", "Unit")]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(2, 1)]
    public void PullRequestRepositoryLoadProgressWhenCountersAreInvalidThrowsArgumentOutOfRangeException(
        int loaded,
        int total)
    {
        // Act
        Action act = () => _ = new PullRequestRepositoryLoadProgress(loaded, total);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Pull request repository progress maps valid counters")]
    [Trait("Category", "Unit")]
    public void PullRequestRepositoryLoadProgressWhenCountersAreValidMapsValues()
    {
        // Act
        var progress = new PullRequestRepositoryLoadProgress(2, 3);

        // Assert
        progress.LoadedRepositories.Should().Be(2);
        progress.TotalRepositories.Should().Be(3);
    }
}
