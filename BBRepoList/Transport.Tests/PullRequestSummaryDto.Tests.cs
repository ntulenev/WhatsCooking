using System.Text.Json;

using FluentAssertions;

namespace BBRepoList.Transport.Tests;

public sealed class PullRequestSummaryDtoTests
{
    [Fact(DisplayName = "Pull request summary DTO maps JSON properties")]
    [Trait("Category", "Unit")]
    public void PullRequestSummaryDtoWhenDeserializedMapsProperties()
    {
        var result = JsonSerializer.Deserialize<PullRequestSummaryDto>("""{"raw":"Summary"}""");

        result.Should().NotBeNull();
        result!.Raw.Should().Be("Summary");
    }
}
