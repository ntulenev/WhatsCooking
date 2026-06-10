using System.Text.Json;

using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class IdentifierModelsTests
{
    [Fact(DisplayName = "Bitbucket id trims value and compares normalized identifiers")]
    [Trait("Category", "Unit")]
    public void BitbucketIdWhenCreatedNormalizesEquality()
    {
        // Arrange
        var first = new BitbucketId(" {User-ID} ");
        var second = new BitbucketId("user-id");

        // Assert
        first.Value.Should().Be("{User-ID}");
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact(DisplayName = "Default Bitbucket ids compare equal")]
    [Trait("Category", "Unit")]
    public void BitbucketIdWhenDefaultComparesUsingEmptyNormalizedValue()
    {
        // Arrange
        var first = default(BitbucketId);
        var second = default(BitbucketId);

        // Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
        first.Value.Should().BeNull();
    }

    [Theory(DisplayName = "Bitbucket id rejects empty values")]
    [Trait("Category", "Unit")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void BitbucketIdWhenValueIsEmptyThrowsArgumentException(string? value)
    {
        // Act
        Action act = () => _ = new BitbucketId(value!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory(DisplayName = "Try create Bitbucket id handles valid and invalid values")]
    [Trait("Category", "Unit")]
    [InlineData(" user ", true, "user")]
    [InlineData("", false, null)]
    [InlineData(" ", false, null)]
    [InlineData(null, false, null)]
    public void TryCreateWhenValueIsProvidedReturnsExpectedResult(
        string? value,
        bool expectedResult,
        string? expectedValue)
    {
        // Act
        var result = BitbucketId.TryCreate(value, out var id);

        // Assert
        result.Should().Be(expectedResult);
        id.Value.Should().Be(expectedValue);
    }

    [Theory(DisplayName = "Workspace and repository slug trim valid values")]
    [Trait("Category", "Unit")]
    [InlineData(true)]
    [InlineData(false)]
    public void ScopedIdentifierWhenValueIsValidTrimsValue(bool useWorkspace)
    {
        // Act
        var result = useWorkspace
            ? new BitbucketWorkspace(" workspace ").ToString()
            : new RepositorySlug(" repository ").ToString();

        // Assert
        result.Should().Be(useWorkspace ? "workspace" : "repository");
    }

    [Theory(DisplayName = "Workspace and repository slug reject empty values")]
    [Trait("Category", "Unit")]
    [InlineData(true)]
    [InlineData(false)]
    public void ScopedIdentifierWhenValueIsEmptyThrowsArgumentException(bool useWorkspace)
    {
        // Act
        Action act = useWorkspace
            ? () => _ = new BitbucketWorkspace(" ")
            : () => _ = new RepositorySlug(" ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory(DisplayName = "Pull request id requires a positive value")]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(-1)]
    public void PullRequestIdWhenValueIsNotPositiveThrowsArgumentOutOfRangeException(int value)
    {
        // Act
        Action act = () => _ = new PullRequestId(value);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Pull request id serializes as a JSON number")]
    [Trait("Category", "Unit")]
    public void PullRequestIdWhenSerializedRoundTripsAsNumber()
    {
        // Arrange
        var id = new PullRequestId(42);

        // Act
        var json = JsonSerializer.Serialize(id);
        var result = JsonSerializer.Deserialize<PullRequestId>(json);

        // Assert
        json.Should().Be("42");
        result.Should().Be(id);
        result.ToString().Should().Be("42");
    }

    [Theory(DisplayName = "Pull request id rejects invalid JSON values")]
    [Trait("Category", "Unit")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("\"42\"")]
    public void PullRequestIdWhenJsonIsInvalidThrowsJsonException(string json)
    {
        // Act
        Action act = () => _ = JsonSerializer.Deserialize<PullRequestId>(json);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact(DisplayName = "User name uses fallback only for null")]
    [Trait("Category", "Unit")]
    public void UserNameWhenCreatedPreservesValueOrUsesFallback()
    {
        // Act
        var missing = new UserName(null);
        var empty = new UserName(string.Empty);

        // Assert
        missing.Value.Should().Be("<N/A>");
        missing.ToString().Should().Be("<N/A>");
        empty.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "Repository search mode exposes supported values")]
    [Trait("Category", "Unit")]
    public void RepositorySearchModeWhenEnumeratedContainsSupportedValues()
    {
        // Act
        var values = Enum.GetValues<RepositorySearchMode>();

        // Assert
        values.Should().Equal(RepositorySearchMode.Contains, RepositorySearchMode.StartWith);
    }
}
