using System.Text.Json;

using FluentAssertions;

namespace BBRepoList.Transport.Tests;

public sealed class PullRequestPageDtoTests
{
    [Fact(DisplayName = "Pull request page DTO maps JSON properties")]
    [Trait("Category", "Unit")]
    public void PullRequestPageDtoWhenDeserializedMapsProperties()
    {
        var result = JsonSerializer.Deserialize<PullRequestPageDto>(
            """{"values":[{"id":42}],"next":"https://api.bitbucket.org/next"}""");

        result.Should().NotBeNull();
        result!.Values.Should().ContainSingle().Which.Id.Should().Be(42);
        result.Next.Should().Be(new Uri("https://api.bitbucket.org/next"));
    }
}
