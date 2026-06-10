using System.Text.Json;

using FluentAssertions;

namespace BBRepoList.Transport.Tests;

public sealed class PullRequestAuthorDtoTests
{
    [Fact(DisplayName = "Pull request author DTO maps JSON properties")]
    [Trait("Category", "Unit")]
    public void PullRequestAuthorDtoWhenDeserializedMapsProperties()
    {
        var result = JsonSerializer.Deserialize<PullRequestAuthorDto>(
            """{"uuid":"author","display_name":"Author Name"}""");

        result.Should().NotBeNull();
        result!.Uuid.Should().Be("author");
        result.DisplayName.Should().Be("Author Name");
    }
}
