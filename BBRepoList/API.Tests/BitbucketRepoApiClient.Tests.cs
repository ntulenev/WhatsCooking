using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Transport;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace BBRepoList.API.Tests;

public sealed class BitbucketRepoApiClientTests
{
    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new BitbucketRepoApiClient(Mock.Of<IBitbucketTransport>(), options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Get repositories reads all pages and maps values")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesWhenPagesExistReturnsMappedRepositories()
    {
        // Arrange
        var firstUrl = new Uri(
            "repositories/workspace?pagelen=25&fields=values.name%2Cvalues.slug%2Cvalues.created_on%2Cvalues.updated_on%2Cnext",
            UriKind.Relative);
        var secondUrl = new Uri("next-page", UriKind.Relative);
        using var cancellation = new CancellationTokenSource();
        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport.Setup(instance => instance.GetAsync<RepoPageDto>(
                firstUrl,
                It.Is<CancellationToken>(token => token == cancellation.Token)))
            .ReturnsAsync(new RepoPageDto(
                [new RepositoryDto("First", Slug: "first")],
                secondUrl));
        transport.Setup(instance => instance.GetAsync<RepoPageDto>(
                secondUrl,
                It.Is<CancellationToken>(token => token == cancellation.Token)))
            .ReturnsAsync(new RepoPageDto(
                [new RepositoryDto("Second", Slug: "second")],
                null));
        var client = new BitbucketRepoApiClient(transport.Object, CreateOptions(pageLen: 25));

        // Act
        var result = await ReadAllAsync(client.GetRepositoriesAsync(default, cancellation.Token));

        // Assert
        result.Select(repository => repository.Name).Should().Equal("First", "Second");
        result.Select(repository => repository.Slug!.Value.Value).Should().Equal("first", "second");
    }

    [Fact(DisplayName = "Get repositories escapes filter value in Bitbucket query")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesWhenFilterExistsEscapesFilterInRequestUri()
    {
        // Arrange
        Uri? requestedUrl = null;
        using var cancellation = new CancellationTokenSource();
        var transport = new Mock<IBitbucketTransport>();
        transport.Setup(instance => instance.GetAsync<RepoPageDto>(
                It.Is<Uri>(url =>
                    Uri.UnescapeDataString(url.OriginalString).EndsWith(
                        "&q=name ~ \"team\\\\\\\"api\"",
                        StringComparison.Ordinal)),
                It.Is<CancellationToken>(token => token == cancellation.Token)))
            .Callback<Uri, CancellationToken>((url, _) => requestedUrl = url)
            .ReturnsAsync((RepoPageDto?)null);
        var client = new BitbucketRepoApiClient(transport.Object, CreateOptions());

        // Act
        var result = await ReadAllAsync(client.GetRepositoriesAsync(
            new FilterPattern("team\\\"api", RepositorySearchMode.Contains),
            cancellation.Token));

        // Assert
        result.Should().BeEmpty();
        Uri.UnescapeDataString(requestedUrl!.OriginalString)
            .Should().EndWith("&q=name ~ \"team\\\\\\\"api\"");
    }

    [Fact(DisplayName = "Get repositories honors cancellation between yielded values")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesWhenCancellationIsRequestedStopsEnumeration()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var expectedUrl = new Uri(
            "repositories/workspace?pagelen=10&fields=values.name%2Cvalues.slug%2Cvalues.created_on%2Cvalues.updated_on%2Cnext",
            UriKind.Relative);
        var transport = new Mock<IBitbucketTransport>();
        transport.Setup(instance => instance.GetAsync<RepoPageDto>(
                It.Is<Uri>(url => url == expectedUrl),
                It.Is<CancellationToken>(token => token == cancellation.Token)))
            .ReturnsAsync(new RepoPageDto(
                [
                    new RepositoryDto("First"),
                    new RepositoryDto("Second")
                ],
                null));
        var client = new BitbucketRepoApiClient(transport.Object, CreateOptions());
        await using var enumerator = client
            .GetRepositoriesAsync(default, cancellation.Token)
            .GetAsyncEnumerator(cancellation.Token);
        _ = await enumerator.MoveNextAsync();
        await cancellation.CancelAsync();

        // Act
        Func<Task> act = async () => _ = await enumerator.MoveNextAsync();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static async Task<List<Repository>> ReadAllAsync(IAsyncEnumerable<Repository> source)
    {
        var result = new List<Repository>();
        await foreach (var repository in source)
        {
            result.Add(repository);
        }

        return result;
    }

    private static IOptions<BitbucketOptions> CreateOptions(int pageLen = 10) =>
        Options.Create(new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.example.test/"),
            Workspace = "workspace",
            PageLen = pageLen
        });
}
