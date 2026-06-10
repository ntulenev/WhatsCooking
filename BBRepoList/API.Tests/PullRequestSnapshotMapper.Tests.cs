using BBRepoList.Abstractions;
using BBRepoList.Models;
using BBRepoList.Transport;

using FluentAssertions;

using Moq;

namespace BBRepoList.API.Tests;

public sealed class PullRequestSnapshotMapperTests
{
    [Fact(DisplayName = "Constructor throws when JSON parser is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenJsonParserIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketJsonParser jsonParser = null!;

        // Act
        Action act = () => _ = new PullRequestSnapshotMapper(
            jsonParser,
            Mock.Of<IPullRequestFingerprintBuilder>());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when fingerprint builder is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenFingerprintBuilderIsNullThrowsArgumentNullException()
    {
        // Arrange
        IPullRequestFingerprintBuilder fingerprintBuilder = null!;

        // Act
        Action act = () => _ = new PullRequestSnapshotMapper(
            Mock.Of<IBitbucketJsonParser>(),
            fingerprintBuilder);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Create snapshot throws when pull request is null")]
    [Trait("Category", "Unit")]
    public void CreateSnapshotWhenPullRequestIsNullThrowsArgumentNullException()
    {
        // Arrange
        var mapper = new PullRequestSnapshotMapper(
            Mock.Of<IBitbucketJsonParser>(),
            Mock.Of<IPullRequestFingerprintBuilder>());
        PullRequestDto pullRequest = null!;

        // Act
        Action act = () => mapper.CreateSnapshot(pullRequest, new BitbucketId("current-user"));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Create snapshot maps fallback text and review state")]
    [Trait("Category", "Unit")]
    public void CreateSnapshotWhenDtoIsValidMapsExpectedValues()
    {
        // Arrange
        var currentUserId = new BitbucketId("{current-user}");
        var createdOn = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var parser = new Mock<IBitbucketJsonParser>(MockBehavior.Strict);
        parser.Setup(instance => instance.IsRequestChangesState("changes_requested")).Returns(true);
        parser.Setup(instance => instance.IsApprovalState(It.Is<PullRequestParticipantDto>(
                participant => participant.User!.Uuid == "reviewer")))
            .Returns(false);
        parser.Setup(instance => instance.IsRequestChangesState("approved")).Returns(false);
        parser.Setup(instance => instance.IsApprovalState(It.Is<PullRequestParticipantDto>(
                participant => participant.User!.Uuid == "current-user")))
            .Returns(true);
        var fingerprintBuilder = new Mock<IPullRequestFingerprintBuilder>(MockBehavior.Strict);
        var pullRequest = new PullRequestDto(
            Id: 17,
            Title: "  ",
            CreatedOn: createdOn,
            Description: " ",
            Summary: new PullRequestSummaryDto(" summary text "),
            Author: new PullRequestAuthorDto("{AUTHOR}", "Author Name"),
            Participants:
            [
                new(new PullRequestAuthorDto("reviewer"), false, "changes_requested"),
                new(new PullRequestAuthorDto("current-user"), true, "approved"),
                new(new PullRequestAuthorDto(null), true, "approved")
            ]);
        fingerprintBuilder.Setup(instance => instance.BuildFingerprint(pullRequest)).Returns("fingerprint");
        var mapper = new PullRequestSnapshotMapper(parser.Object, fingerprintBuilder.Object);

        // Act
        var result = mapper.CreateSnapshot(pullRequest, currentUserId);

        // Assert
        result.Id.Should().Be(new PullRequestId(17));
        result.Title.Should().Be("PR-17");
        result.CreatedOn.Should().Be(createdOn);
        result.DescriptionText.Should().Be(" summary text ");
        result.AuthorId.Should().Be(new BitbucketId("author"));
        result.AuthorDisplayName.Should().Be("Author Name");
        result.RequestChangesCount.Should().Be(1);
        result.HasCurrentUserRequestChanges.Should().BeFalse();
        result.ApprovalsCount.Should().Be(1);
        result.HasCurrentUserApproval.Should().BeTrue();
        result.CacheFingerprint.Should().Be("fingerprint");
    }

    [Fact(DisplayName = "Create snapshot prefers trimmed title and description")]
    [Trait("Category", "Unit")]
    public void CreateSnapshotWhenTitleAndDescriptionExistUsesThem()
    {
        // Arrange
        var pullRequest = new PullRequestDto(
            Id: 3,
            Title: " Feature ",
            CreatedOn: DateTimeOffset.UtcNow,
            Description: "Description",
            Summary: new PullRequestSummaryDto("Summary"));
        var fingerprintBuilder = new Mock<IPullRequestFingerprintBuilder>();
        fingerprintBuilder.Setup(instance => instance.BuildFingerprint(pullRequest)).Returns("fingerprint");
        var mapper = new PullRequestSnapshotMapper(
            Mock.Of<IBitbucketJsonParser>(),
            fingerprintBuilder.Object);

        // Act
        var result = mapper.CreateSnapshot(pullRequest, new BitbucketId("current-user"));

        // Assert
        result.Title.Should().Be("Feature");
        result.DescriptionText.Should().Be("Description");
        result.AuthorId.Should().BeNull();
    }
}
