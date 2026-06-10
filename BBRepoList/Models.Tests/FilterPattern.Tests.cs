using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class FilterPatternTests
{
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
        var filter = new FilterPattern(phrase, searchMode);
        var repository = new Repository("Payments API");

        var result = filter.Filter(repository);

        result.Should().Be(expected);
        filter.HasFilter.Should().Be(!string.IsNullOrWhiteSpace(phrase));
    }

    [Fact(DisplayName = "Filter pattern rejects null repository")]
    [Trait("Category", "Unit")]
    public void FilterWhenRepositoryIsNullThrowsArgumentNullException()
    {
        var filter = new FilterPattern("api");
        Repository repository = null!;

        Action act = () => filter.Filter(repository);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Filter pattern rejects unsupported search mode")]
    [Trait("Category", "Unit")]
    public void FilterWhenSearchModeIsUnsupportedThrowsInvalidOperationException()
    {
        var filter = new FilterPattern("api", (RepositorySearchMode)42);

        Action act = () => filter.Filter(new Repository("API"));

        act.Should().Throw<InvalidOperationException>();
    }
}
