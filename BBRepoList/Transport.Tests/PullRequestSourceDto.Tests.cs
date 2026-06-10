using System.Text.Json;

using FluentAssertions;

namespace BBRepoList.Transport.Tests;

public sealed class PullRequestSourceDtoTests
{
    [Fact(DisplayName = "Pull request source DTO maps JSON properties")]
    [Trait("Category", "Unit")]
    public void PullRequestSourceDtoWhenDeserializedMapsProperties()
    {
        var result = JsonSerializer.Deserialize<PullRequestSourceDto>(
            """{"commit":{"hash":"abc123"}}""");

        result.Should().NotBeNull();
        result!.Commit.Should().NotBeNull();
        result.Commit!.Hash.Should().Be("abc123");
    }
}
