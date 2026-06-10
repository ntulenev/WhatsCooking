using System.Text.Json;

using BBRepoList.Models;
using BBRepoList.Transport;

using FluentAssertions;

namespace BBRepoList.API.Helpers.Tests;

public sealed class BitbucketJsonParserTests
{
    [Fact(DisplayName = "TryReadUuidFromObject reads and normalizes UUID")]
    [Trait("Category", "Unit")]
    public void TryReadUuidFromObjectWhenUuidIsValidReturnsIdentifier()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var element = ParseJson("""{ "uuid": " {User-ID} " }""");

        // Act
        var result = parser.TryReadUuidFromObject(element, out var uuid);

        // Assert
        result.Should().BeTrue();
        uuid.Should().Be(new BitbucketId("user-id"));
    }

    [Theory(DisplayName = "TryReadUuidFromObject rejects invalid JSON shapes")]
    [Trait("Category", "Unit")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("""{ "uuid": 17 }""")]
    [InlineData("""{ "uuid": "   " }""")]
    public void TryReadUuidFromObjectWhenUuidIsInvalidReturnsFalse(string json)
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var element = ParseJson(json);

        // Act
        var result = parser.TryReadUuidFromObject(element, out var uuid);

        // Assert
        result.Should().BeFalse();
        uuid.Should().Be(default(BitbucketId));
    }

    [Fact(DisplayName = "TryReadDateTime parses invariant timestamp")]
    [Trait("Category", "Unit")]
    public void TryReadDateTimeWhenTimestampIsValidReturnsValue()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var element = JsonSerializer.SerializeToElement(" 2026-06-01T12:30:00+02:00 ");

        // Act
        var result = parser.TryReadDateTime(element, out var value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be(new DateTimeOffset(2026, 6, 1, 12, 30, 0, TimeSpan.FromHours(2)));
    }

    [Theory(DisplayName = "TryReadDateTime rejects invalid JSON values")]
    [Trait("Category", "Unit")]
    [InlineData("null")]
    [InlineData("17")]
    [InlineData("""{ "date": "2026-06-01" }""")]
    [InlineData("\"not-a-date\"")]
    public void TryReadDateTimeWhenValueIsInvalidReturnsFalse(string json)
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var element = ParseJson(json);

        // Act
        var result = parser.TryReadDateTime(element, out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().Be(default(DateTimeOffset));
    }

    [Fact(DisplayName = "AddActivityEntriesFromJson throws when callback is null")]
    [Trait("Category", "Unit")]
    public void AddActivityEntriesFromJsonWhenCallbackIsNullThrowsArgumentNullException()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var element = ParseJson("{}");
        Action<BitbucketId, DateTimeOffset, bool> onEntry = null!;

        // Act
        Action act = () => parser.AddActivityEntriesFromJson(element, false, onEntry);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "AddActivityEntriesFromJson reads nested activity entries")]
    [Trait("Category", "Unit")]
    public void AddActivityEntriesFromJsonWhenPayloadIsNestedReturnsExactEntries()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var element = ParseJson(
            """
            [
              {
                "actor": { "uuid": "{actor}" },
                "date": "2026-06-01T10:00:00Z"
              },
              {
                "comment": {
                  "user": { "uuid": "commenter" },
                  "created_on": "2026-06-01T11:00:00Z"
                }
              },
              {
                "wrapper": [
                  {
                    "user": { "uuid": "reviewer" },
                    "updated_on": "2026-06-01T12:00:00Z"
                  },
                  {
                    "actor": { "uuid": "missing-date" }
                  }
                ]
              }
            ]
            """);
        var entries = new List<(BitbucketId ActorId, DateTimeOffset HappenedOn, bool IsComment)>();

        // Act
        parser.AddActivityEntriesFromJson(
            element,
            false,
            (actorId, happenedOn, isComment) =>
                entries.Add((actorId, happenedOn, isComment)));

        // Assert
        entries.Should().Equal(
            (new BitbucketId("actor"), CreateDate(10), false),
            (new BitbucketId("commenter"), CreateDate(11), true),
            (new BitbucketId("reviewer"), CreateDate(12), false));
    }

    [Fact(DisplayName = "AddActivityEntriesFromJson preserves comment context")]
    [Trait("Category", "Unit")]
    public void AddActivityEntriesFromJsonWhenContextIsCommentMarksEntriesAsComments()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var element = ParseJson(
            """
            {
              "actor": { "uuid": "actor" },
              "date": "2026-06-01T10:00:00Z"
            }
            """);
        var entries = new List<(BitbucketId ActorId, DateTimeOffset HappenedOn, bool IsComment)>();

        // Act
        parser.AddActivityEntriesFromJson(
            element,
            true,
            (actorId, happenedOn, isComment) =>
                entries.Add((actorId, happenedOn, isComment)));

        // Assert
        entries.Should().Equal((new BitbucketId("actor"), CreateDate(10), true));
    }

    [Theory(DisplayName = "IsRequestChangesState recognizes supported variants")]
    [Trait("Category", "Unit")]
    [InlineData("changes_requested")]
    [InlineData("CHANGES REQUESTED")]
    [InlineData("changes-requested")]
    [InlineData("requested_changes")]
    [InlineData("request_changes")]
    [InlineData("needs_work")]
    [InlineData("Needs Work")]
    public void IsRequestChangesStateWhenStateIsSupportedReturnsTrue(string state)
    {
        // Arrange
        var parser = new BitbucketJsonParser();

        // Act
        var result = parser.IsRequestChangesState(state);

        // Assert
        result.Should().BeTrue();
    }

    [Theory(DisplayName = "IsRequestChangesState rejects unsupported values")]
    [Trait("Category", "Unit")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("approved")]
    [InlineData("changes")]
    public void IsRequestChangesStateWhenStateIsUnsupportedReturnsFalse(string? state)
    {
        // Arrange
        var parser = new BitbucketJsonParser();

        // Act
        var result = parser.IsRequestChangesState(state);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "IsApprovalState throws when participant is null")]
    [Trait("Category", "Unit")]
    public void IsApprovalStateWhenParticipantIsNullThrowsArgumentNullException()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        PullRequestParticipantDto participant = null!;

        // Act
        Action act = () => parser.IsApprovalState(participant);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory(DisplayName = "IsApprovalState evaluates approved flag and state")]
    [Trait("Category", "Unit")]
    [InlineData(true, null, true)]
    [InlineData(false, "APPROVED", true)]
    [InlineData(null, "approved", true)]
    [InlineData(false, "changes_requested", false)]
    [InlineData(null, null, false)]
    public void IsApprovalStateWhenParticipantExistsReturnsExpectedResult(
        bool? approved,
        string? state,
        bool expected)
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var participant = new PullRequestParticipantDto(Approved: approved, State: state);

        // Act
        var result = parser.IsApprovalState(participant);

        // Assert
        result.Should().Be(expected);
    }

    private static JsonElement ParseJson(string json) =>
        JsonSerializer.Deserialize<JsonElement>(json);

    private static DateTimeOffset CreateDate(int hour) =>
        new(2026, 6, 1, hour, 0, 0, TimeSpan.Zero);
}
