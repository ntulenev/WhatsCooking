using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class BitbucketIdTests
{
    [Fact(DisplayName = "Bitbucket id trims value and compares normalized identifiers")]
    [Trait("Category", "Unit")]
    public void BitbucketIdWhenCreatedNormalizesEquality()
    {
        var first = new BitbucketId(" {User-ID} ");
        var second = new BitbucketId("user-id");

        first.Value.Should().Be("{User-ID}");
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact(DisplayName = "Default Bitbucket ids compare equal")]
    [Trait("Category", "Unit")]
    public void BitbucketIdWhenDefaultComparesUsingEmptyNormalizedValue()
    {
        var first = default(BitbucketId);
        var second = default(BitbucketId);

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
        Action act = () => _ = new BitbucketId(value!);

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
        var result = BitbucketId.TryCreate(value, out var id);

        result.Should().Be(expectedResult);
        id.Value.Should().Be(expectedValue);
    }
}
