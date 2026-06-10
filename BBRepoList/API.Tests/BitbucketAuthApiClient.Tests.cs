using System.Text;

using BBRepoList.Abstractions;
using BBRepoList.Models;
using BBRepoList.Transport;

using FluentAssertions;

using Moq;

namespace BBRepoList.API.Tests;

public sealed class BitbucketAuthApiClientTests
{
    [Fact(DisplayName = "Constructor throws when transport is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTransportIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketTransport transport = null!;

        // Act
        Action act = () => _ = new BitbucketAuthApiClient(transport);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Auth self check requests current user and maps response")]
    [Trait("Category", "Unit")]
    public async Task AuthSelfCheckWhenResponseExistsReturnsMappedUser()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var cancellationToken = cancellation.Token;
        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport.Setup(instance => instance.GetAsync<BitbucketUserDto>(
                new Uri("user", UriKind.Relative),
                cancellationToken))
            .ReturnsAsync(new BitbucketUserDto("{user-id}", "User Name"));
        var client = new BitbucketAuthApiClient(transport.Object);

        // Act
        var result = await client.AuthSelfCheckAsync(cancellationToken);

        // Assert
        result.Uuid.Should().Be(new BitbucketId("user-id"));
        result.DisplayName.Should().Be(new UserName("User Name"));
    }

    [Fact(DisplayName = "Auth self check throws when response is empty")]
    [Trait("Category", "Unit")]
    public async Task AuthSelfCheckWhenResponseIsNullThrowsInvalidOperationException()
    {
        // Arrange
        var transport = new Mock<IBitbucketTransport>();
        transport.Setup(instance => instance.GetAsync<BitbucketUserDto>(
                It.Is<Uri>(url => url == new Uri("user", UriKind.Relative)),
                It.Is<CancellationToken>(token => token == CancellationToken.None)))
            .ReturnsAsync((BitbucketUserDto?)null);
        var client = new BitbucketAuthApiClient(transport.Object);

        // Act
        Func<Task> act = () => client.AuthSelfCheckAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact(DisplayName = "Build auth header encodes email and token as basic credentials")]
    [Trait("Category", "Unit")]
    public void BuildAuthHeaderWhenCredentialsAreValidReturnsBasicHeader()
    {
        // Arrange
        IBitbucketAuthApiClient client = new BitbucketAuthApiClient(Mock.Of<IBitbucketTransport>());

        // Act
        var result = client.BuildAuthHeader("user@example.com", "token-value");

        // Assert
        result.Scheme.Should().Be("Basic");
        result.Parameter.Should().Be(Convert.ToBase64String(
            Encoding.UTF8.GetBytes("user@example.com:token-value")));
    }
}
