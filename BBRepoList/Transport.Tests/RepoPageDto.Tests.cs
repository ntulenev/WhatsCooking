using System.Text.Json;

using FluentAssertions;

namespace BBRepoList.Transport.Tests;

public sealed class RepoPageDtoTests
{
    [Fact(DisplayName = "Repository page DTO maps JSON properties")]
    [Trait("Category", "Unit")]
    public void RepoPageDtoWhenDeserializedMapsProperties()
    {
        var result = JsonSerializer.Deserialize<RepoPageDto>(
            """{"values":[{"name":"Repository"}],"next":"https://api.bitbucket.org/next"}""");

        result.Should().NotBeNull();
        result!.Values.Should().ContainSingle()
            .Which.Name.Should().Be("Repository");
        result.Next.Should().Be(new Uri("https://api.bitbucket.org/next"));
    }
}
