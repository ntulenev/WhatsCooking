using System.Text.Json;

using FluentAssertions;

namespace BBRepoList.Transport.Tests;

public sealed class PullRequestDtoTests
{
    [Fact(DisplayName = "Pull request DTO maps JSON properties")]
    [Trait("Category", "Unit")]
    public void PullRequestDtoWhenDeserializedMapsProperties()
    {
        var result = JsonSerializer.Deserialize<PullRequestDto>(
            """
            {
              "id":42,
              "title":"Title",
              "created_on":"2026-06-01T10:00:00Z",
              "updated_on":"2026-06-02T11:00:00Z",
              "state":"OPEN",
              "description":"Description",
              "summary":{"raw":"Summary"},
              "author":{"uuid":"author","display_name":"Author"},
              "source":{"commit":{"hash":"abc123"}},
              "comment_count":3,
              "task_count":2,
              "participants":[{"approved":true,"state":"approved"}]
            }
            """);

        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Title.Should().Be("Title");
        result.CreatedOn.Should().Be(new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));
        result.UpdatedOn.Should().Be(new DateTimeOffset(2026, 6, 2, 11, 0, 0, TimeSpan.Zero));
        result.State.Should().Be("OPEN");
        result.Description.Should().Be("Description");
        result.Summary.Should().Be(new PullRequestSummaryDto("Summary"));
        result.Author.Should().Be(new PullRequestAuthorDto("author", "Author"));
        result.Source.Should().Be(new PullRequestSourceDto(new PullRequestCommitDto("abc123")));
        result.CommentCount.Should().Be(3);
        result.TaskCount.Should().Be(2);
        result.Participants.Should().ContainSingle()
            .Which.Should().Be(new PullRequestParticipantDto(Approved: true, State: "approved"));
    }
}
