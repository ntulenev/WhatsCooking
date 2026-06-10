using System.Text.Json;

using FluentAssertions;

namespace BBRepoList.Transport.Tests;

public sealed class RepositoryDtoTests
{
    [Fact(DisplayName = "Repository DTO maps JSON properties")]
    [Trait("Category", "Unit")]
    public void RepositoryDtoWhenDeserializedMapsProperties()
    {
        var result = JsonSerializer.Deserialize<RepositoryDto>(
            """
            {
              "name":"Repository",
              "created_on":"2026-06-01T10:00:00Z",
              "updated_on":"2026-06-02T11:00:00Z",
              "slug":"repository"
            }
            """);

        result.Should().Be(new RepositoryDto(
            "Repository",
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 2, 11, 0, 0, TimeSpan.Zero),
            "repository"));
    }
}
