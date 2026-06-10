using System.Text.Json;

using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class PullRequestIdJsonConverterTests
{
    [Fact(DisplayName = "Pull request id converter serializes as a JSON number")]
    [Trait("Category", "Unit")]
    public void PullRequestIdJsonConverterWhenValueIsValidRoundTripsAsNumber()
    {
        var id = new PullRequestId(42);

        var json = JsonSerializer.Serialize(id);
        var result = JsonSerializer.Deserialize<PullRequestId>(json);

        json.Should().Be("42");
        result.Should().Be(id);
    }

    [Theory(DisplayName = "Pull request id converter rejects invalid JSON values")]
    [Trait("Category", "Unit")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("\"42\"")]
    public void PullRequestIdJsonConverterWhenJsonIsInvalidThrowsJsonException(string json)
    {
        Action act = () => _ = JsonSerializer.Deserialize<PullRequestId>(json);

        act.Should().Throw<JsonException>();
    }
}
