using BBRepoList.Transport;

using FluentAssertions;

namespace BBRepoList.API.Tests;

public sealed class PullRequestFingerprintBuilderTests
{
    [Fact(DisplayName = "Build fingerprint throws when pull request is null")]
    [Trait("Category", "Unit")]
    public void BuildFingerprintWhenPullRequestIsNullThrowsArgumentNullException()
    {
        // Arrange
        var builder = new PullRequestFingerprintBuilder();
        PullRequestDto pullRequest = null!;

        // Act
        Action act = () => builder.BuildFingerprint(pullRequest);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Build fingerprint returns uppercase SHA-256 value")]
    [Trait("Category", "Unit")]
    public void BuildFingerprintWhenPullRequestIsValidReturnsSha256Value()
    {
        // Arrange
        var builder = new PullRequestFingerprintBuilder();
        var pullRequest = CreatePullRequest();

        // Act
        var result = builder.BuildFingerprint(pullRequest);

        // Assert
        result.Should().MatchRegex("^[0-9A-F]{64}$");
    }

    [Fact(DisplayName = "Build fingerprint ignores participant order and surrounding whitespace")]
    [Trait("Category", "Unit")]
    public void BuildFingerprintWhenEquivalentPullRequestsDifferInFormattingReturnsSameValue()
    {
        // Arrange
        var builder = new PullRequestFingerprintBuilder();
        var first = CreatePullRequest(
            participants:
            [
                new(new PullRequestAuthorDto(" user-b "), false, " changes_requested "),
                new(new PullRequestAuthorDto("user-a"), true, "approved")
            ]);
        var second = CreatePullRequest(
            state: " OPEN ",
            commitHash: " abc123 ",
            participants:
            [
                new(new PullRequestAuthorDto("user-a"), true, "approved"),
                new(new PullRequestAuthorDto("user-b"), false, "changes_requested")
            ]);

        // Act
        var firstResult = builder.BuildFingerprint(first);
        var secondResult = builder.BuildFingerprint(second);

        // Assert
        firstResult.Should().Be(secondResult);
    }

    [Fact(DisplayName = "Build fingerprint changes when tracked pull request state changes")]
    [Trait("Category", "Unit")]
    public void BuildFingerprintWhenTrackedValueChangesReturnsDifferentValue()
    {
        // Arrange
        var builder = new PullRequestFingerprintBuilder();
        var first = CreatePullRequest(commentCount: 3);
        var second = CreatePullRequest(commentCount: 4);

        // Act
        var firstResult = builder.BuildFingerprint(first);
        var secondResult = builder.BuildFingerprint(second);

        // Assert
        firstResult.Should().NotBe(secondResult);
    }

    private static PullRequestDto CreatePullRequest(
        string state = "OPEN",
        string commitHash = "abc123",
        int commentCount = 3,
        ICollection<PullRequestParticipantDto>? participants = null) =>
        new(
            Id: 42,
            UpdatedOn: new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero),
            State: state,
            Source: new PullRequestSourceDto(new PullRequestCommitDto(commitHash)),
            CommentCount: commentCount,
            TaskCount: 2,
            Participants: participants ??
            [
                new(new PullRequestAuthorDto("user-a"), true, "approved"),
                new(new PullRequestAuthorDto("user-b"), false, "changes_requested")
            ]);
}
