using System.Text.Json;

using FluentAssertions;

namespace BBRepoList.Transport.Tests;

public sealed class PullRequestCommitDtoTests
{
    [Fact(DisplayName = "Pull request commit DTO maps JSON properties")]
    [Trait("Category", "Unit")]
    public void PullRequestCommitDtoWhenDeserializedMapsProperties()
    {
        var result = JsonSerializer.Deserialize<PullRequestCommitDto>("""{"hash":"abc123"}""");

        result.Should().NotBeNull();
        result!.Hash.Should().Be("abc123");
    }
}
