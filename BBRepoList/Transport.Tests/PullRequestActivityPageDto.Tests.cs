using System.Text.Json;

using FluentAssertions;

namespace BBRepoList.Transport.Tests;

public sealed class PullRequestActivityPageDtoTests
{
    [Fact(DisplayName = "Pull request activity page DTO maps JSON properties")]
    [Trait("Category", "Unit")]
    public void PullRequestActivityPageDtoWhenDeserializedMapsProperties()
    {
        var result = JsonSerializer.Deserialize<PullRequestActivityPageDto>(
            """{"values":[{"comment":{"id":1}}],"next":"https://api.bitbucket.org/next"}""");

        result.Should().NotBeNull();
        result!.Values.Should().ContainSingle()
            .Which.Properties.Should().ContainKey("comment");
        result.Next.Should().Be(new Uri("https://api.bitbucket.org/next"));
    }
}
