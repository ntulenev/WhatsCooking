using System.Text.Json;

using FluentAssertions;

namespace BBRepoList.Transport.Tests;

public sealed class PullRequestParticipantDtoTests
{
    [Fact(DisplayName = "Pull request participant DTO maps JSON properties")]
    [Trait("Category", "Unit")]
    public void PullRequestParticipantDtoWhenDeserializedMapsProperties()
    {
        var result = JsonSerializer.Deserialize<PullRequestParticipantDto>(
            """{"user":{"uuid":"reviewer"},"approved":true,"state":"approved"}""");

        result.Should().NotBeNull();
        result!.User.Should().NotBeNull();
        result.User!.Uuid.Should().Be("reviewer");
        result.Approved.Should().BeTrue();
        result.State.Should().Be("approved");
    }
}
