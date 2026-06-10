using System.Text.Json;

using FluentAssertions;

namespace BBRepoList.Transport.Tests;

public sealed class PullRequestPageSummaryDtoTests
{
    [Fact(DisplayName = "Pull request page summary DTO maps JSON properties")]
    [Trait("Category", "Unit")]
    public void PullRequestPageSummaryDtoWhenDeserializedMapsProperties()
    {
        var result = JsonSerializer.Deserialize<PullRequestPageSummaryDto>("""{"size":7}""");

        result.Should().NotBeNull();
        result!.Size.Should().Be(7);
    }
}
