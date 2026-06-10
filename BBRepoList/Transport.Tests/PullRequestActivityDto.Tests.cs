using System.Text.Json;

using FluentAssertions;

namespace BBRepoList.Transport.Tests;

public sealed class PullRequestActivityDtoTests
{
    [Fact(DisplayName = "Pull request activity DTO captures extension data")]
    [Trait("Category", "Unit")]
    public void PullRequestActivityDtoWhenDeserializedCapturesProperties()
    {
        var result = JsonSerializer.Deserialize<PullRequestActivityDto>(
            """{"comment":{"id":1},"approval":{"date":"2026-06-01"}}""");

        result.Should().NotBeNull();
        result!.Properties.Should().ContainKeys("comment", "approval");
        result.Properties!["comment"].GetProperty("id").GetInt32().Should().Be(1);
    }
}
