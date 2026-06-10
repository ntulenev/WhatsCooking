using System.Net;

using BBRepoList.Configuration;

using FluentAssertions;

using Microsoft.Extensions.Options;

namespace BBRepoList.Transport.Tests;

public sealed class BitbucketRetryPolicyTests
{
    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        IOptions<BitbucketOptions> options = null!;

        Action act = () => _ = new BitbucketRetryPolicy(options);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory(DisplayName = "Retry policy rejects attempts outside configured range")]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(4)]
    public void TryGetDelayWhenAttemptIsOutsideRangeReturnsFalse(int retryAttempt)
    {
        var policy = CreatePolicy(retryCount: 3);

        var result = policy.TryGetDelay(retryAttempt, HttpStatusCode.InternalServerError, null, out var delay);

        result.Should().BeFalse();
        delay.Should().Be(TimeSpan.Zero);
    }

    [Theory(DisplayName = "Retry policy retries transient status codes")]
    [Trait("Category", "Unit")]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    public void TryGetDelayWhenStatusIsTransientReturnsDelay(HttpStatusCode statusCode)
    {
        var policy = CreatePolicy(retryCount: 3);

        var result = policy.TryGetDelay(2, statusCode, null, out var delay);

        result.Should().BeTrue();
        delay.Should().Be(TimeSpan.FromMilliseconds(400));
    }

    [Fact(DisplayName = "Retry policy retries HTTP request exceptions")]
    [Trait("Category", "Unit")]
    public void TryGetDelayWhenHttpRequestExceptionOccursReturnsDelay()
    {
        var policy = CreatePolicy(retryCount: 3);

        var result = policy.TryGetDelay(3, null, new HttpRequestException("Failed"), out var delay);

        result.Should().BeTrue();
        delay.Should().Be(TimeSpan.FromMilliseconds(600));
    }

    [Theory(DisplayName = "Retry policy rejects non-transient failures")]
    [Trait("Category", "Unit")]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public void TryGetDelayWhenFailureIsNotTransientReturnsFalse(HttpStatusCode statusCode)
    {
        var policy = CreatePolicy(retryCount: 3);

        var result = policy.TryGetDelay(1, statusCode, new InvalidOperationException(), out var delay);

        result.Should().BeFalse();
        delay.Should().Be(TimeSpan.Zero);
    }

    private static BitbucketRetryPolicy CreatePolicy(int retryCount) =>
        new(Options.Create(new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/"),
            RetryCount = retryCount
        }));
}
