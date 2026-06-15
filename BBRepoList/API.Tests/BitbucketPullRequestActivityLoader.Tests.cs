using System.Text.Json;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Transport;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace BBRepoList.API.Tests;

public sealed class BitbucketPullRequestActivityLoaderTests
{
    [Fact(DisplayName = "Get activities reads every page and returns distinct entries")]
    [Trait("Category", "Unit")]
    public async Task GetActivitiesWhenPagesExistReturnsDistinctParsedEntries()
    {
        // Arrange
        var firstUrl = new Uri(
            "repositories/workspace/repo%20slug/pullrequests/17/activity?pagelen=25&fields=values.actor.uuid%2Cvalues.user.uuid%2Cvalues.date%2Cvalues.created_on%2Cvalues.updated_on%2Cvalues.comment%2Cvalues.approval%2Cvalues.request_changes%2Cvalues.changes_requested%2Cvalues.update%2Cnext",
            UriKind.Relative);
        var secondUrl = new Uri("next-page", UriKind.Relative);
        using var cancellation = new CancellationTokenSource();
        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport.Setup(instance => instance.GetAsync<PullRequestActivityPageDto>(
                firstUrl,
                It.Is<CancellationToken>(token => token == cancellation.Token)))
            .ReturnsAsync(CreatePage(secondUrl, ("comment", "first"), ("approval", "duplicate")));
        transport.Setup(instance => instance.GetAsync<PullRequestActivityPageDto>(
                secondUrl,
                It.Is<CancellationToken>(token => token == cancellation.Token)))
            .ReturnsAsync(CreatePage(null, ("update", "last")));
        var actorId = new BitbucketId("actor");
        var happenedOn = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var parser = new Mock<IBitbucketJsonParser>(MockBehavior.Strict);
        parser.Setup(instance => instance.AddActivityEntriesFromJson(
                It.Is<JsonElement>(element => element.GetString() == "first"),
                It.Is<bool>(isComment => isComment),
                It.IsAny<Action<BitbucketId, DateTimeOffset, bool>>()))
            .Callback<JsonElement, bool, Action<BitbucketId, DateTimeOffset, bool>>(
                (element, isComment, onEntry) => onEntry(actorId, happenedOn, isComment));
        parser.Setup(instance => instance.AddActivityEntriesFromJson(
                It.Is<JsonElement>(element => element.GetString() == "duplicate"),
                It.Is<bool>(isComment => !isComment),
                It.IsAny<Action<BitbucketId, DateTimeOffset, bool>>()))
            .Callback<JsonElement, bool, Action<BitbucketId, DateTimeOffset, bool>>(
                (element, isComment, onEntry) => onEntry(actorId, happenedOn, isComment));
        parser.Setup(instance => instance.AddActivityEntriesFromJson(
                It.Is<JsonElement>(element => element.GetString() == "last"),
                It.Is<bool>(isComment => !isComment),
                It.IsAny<Action<BitbucketId, DateTimeOffset, bool>>()))
            .Callback<JsonElement, bool, Action<BitbucketId, DateTimeOffset, bool>>(
                (element, isComment, onEntry) => onEntry(actorId, happenedOn.AddHours(1), isComment));
        var loader = new BitbucketPullRequestActivityLoader(
            transport.Object,
            parser.Object,
            CreateOptions(pageLen: 25));

        // Act
        var result = await loader.GetActivitiesAsync(
            new RepositorySlug("repo slug"),
            new PullRequestId(17),
            cancellation.Token);

        // Assert
        result.Should().BeEquivalentTo(
        [
            new PullRequestActivityEntry(actorId, happenedOn, true),
            new PullRequestActivityEntry(actorId, happenedOn, false),
            new PullRequestActivityEntry(actorId, happenedOn.AddHours(1), false)
        ]);
    }

    [Fact(DisplayName = "Get activities stops when transport returns empty page")]
    [Trait("Category", "Unit")]
    public async Task GetActivitiesWhenPageIsNullReturnsEmptyList()
    {
        // Arrange
        var expectedUrl = new Uri(
            "repositories/workspace/repository/pullrequests/1/activity?pagelen=10&fields=values.actor.uuid%2Cvalues.user.uuid%2Cvalues.date%2Cvalues.created_on%2Cvalues.updated_on%2Cvalues.comment%2Cvalues.approval%2Cvalues.request_changes%2Cvalues.changes_requested%2Cvalues.update%2Cnext",
            UriKind.Relative);
        using var cancellation = new CancellationTokenSource();
        var transport = new Mock<IBitbucketTransport>();
        transport.Setup(instance => instance.GetAsync<PullRequestActivityPageDto>(
                It.Is<Uri>(url => url == expectedUrl),
                It.Is<CancellationToken>(token => token == cancellation.Token)))
            .ReturnsAsync((PullRequestActivityPageDto?)null);
        var loader = new BitbucketPullRequestActivityLoader(
            transport.Object,
            Mock.Of<IBitbucketJsonParser>(),
            CreateOptions());

        // Act
        var result = await loader.GetActivitiesAsync(
            new RepositorySlug("repository"),
            new PullRequestId(1),
            cancellation.Token);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "Get activities ignores values without properties")]
    [Trait("Category", "Unit")]
    public async Task GetActivitiesWhenValueHasNoPropertiesReturnsEmptyList()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var transport = new Mock<IBitbucketTransport>();
        transport.Setup(instance => instance.GetAsync<PullRequestActivityPageDto>(
                It.IsAny<Uri>(),
                It.Is<CancellationToken>(token => token == cancellation.Token)))
            .ReturnsAsync(new PullRequestActivityPageDto(
                [new PullRequestActivityDto { Properties = null }],
                null));
        var parser = new Mock<IBitbucketJsonParser>(MockBehavior.Strict);
        var loader = new BitbucketPullRequestActivityLoader(
            transport.Object,
            parser.Object,
            CreateOptions());

        // Act
        var result = await loader.GetActivitiesAsync(
            new RepositorySlug("repository"),
            new PullRequestId(1),
            cancellation.Token);

        // Assert
        result.Should().BeEmpty();
        parser.VerifyNoOtherCalls();
    }

    private static PullRequestActivityPageDto CreatePage(
        Uri? next,
        params (string Property, string Value)[] values) =>
        new(
            [.. values.Select(value => new PullRequestActivityDto
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    [value.Property] = JsonSerializer.SerializeToElement(value.Value)
                }
            })],
            next);

    private static IOptions<BitbucketOptions> CreateOptions(int pageLen = 10) =>
        Options.Create(new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.example.test/"),
            Workspace = "workspace",
            PageLen = pageLen
        });
}
