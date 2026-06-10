using System.Net;
using System.Text;

using BBRepoList.Abstractions;

using FluentAssertions;

using Moq;

namespace BBRepoList.Transport.Tests;

public sealed class BitbucketTransportTests
{
    [Fact(DisplayName = "Constructor throws when HTTP client is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenHttpClientIsNullThrowsArgumentNullException()
    {
        HttpClient http = null!;

        Action act = () => _ = new BitbucketTransport(
            http,
            Mock.Of<IBitbucketRetryPolicy>(),
            Mock.Of<IBitbucketTelemetryService>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when retry policy is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenRetryPolicyIsNullThrowsArgumentNullException()
    {
        IBitbucketRetryPolicy retryPolicy = null!;
        using var http = new HttpClient();

        Action act = () => _ = new BitbucketTransport(
            http,
            retryPolicy,
            Mock.Of<IBitbucketTelemetryService>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when telemetry service is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTelemetryServiceIsNullThrowsArgumentNullException()
    {
        IBitbucketTelemetryService telemetryService = null!;
        using var http = new HttpClient();

        Action act = () => _ = new BitbucketTransport(
            http,
            Mock.Of<IBitbucketRetryPolicy>(),
            telemetryService);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Get async throws when URL is null")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenUrlIsNullThrowsArgumentNullException()
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new SequenceHttpMessageHandler();
        using var http = new HttpClient(handler);
        var transport = CreateTransport(http);
        Uri url = null!;

        Func<Task> act = () => transport.GetAsync<TestDto>(url, cancellation.Token);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "Get async returns deserialized response and tracks request")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenResponseIsSuccessfulReturnsDto()
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new SequenceHttpMessageHandler(
            CreateResponse(HttpStatusCode.OK, """{"Value":"result"}"""));
        using var http = new HttpClient(handler);
        var url = new Uri("https://api.bitbucket.org/2.0/user");
        var telemetry = new Mock<IBitbucketTelemetryService>(MockBehavior.Strict);
        telemetry.Setup(instance => instance.TrackRequest(url));
        var transport = CreateTransport(http, telemetryService: telemetry.Object);

        var result = await transport.GetAsync<TestDto>(url, cancellation.Token);

        result.Should().Be(new TestDto("result"));
        handler.RequestTokens.Should().ContainSingle()
            .Which.CanBeCanceled.Should().BeTrue();
        telemetry.VerifyAll();
    }

    [Fact(DisplayName = "Get async returns null for JSON null")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenResponseContainsJsonNullReturnsNull()
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new SequenceHttpMessageHandler(CreateResponse(HttpStatusCode.OK, "null"));
        using var http = new HttpClient(handler);
        var transport = CreateTransport(http);

        var result = await transport.GetAsync<TestDto>(
            new Uri("https://api.bitbucket.org/2.0/user"),
            cancellation.Token);

        result.Should().BeNull();
    }

    [Fact(DisplayName = "Get async throws detailed error for non-retryable response")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenResponseIsNotRetryableThrowsHttpRequestException()
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new SequenceHttpMessageHandler(
            CreateResponse(HttpStatusCode.BadRequest, "invalid", "Bad Request"));
        using var http = new HttpClient(handler);
        var retryPolicy = new Mock<IBitbucketRetryPolicy>(MockBehavior.Strict);
        retryPolicy.Setup(instance => instance.TryGetDelay(
                1,
                HttpStatusCode.BadRequest,
                null,
                out It.Ref<TimeSpan>.IsAny))
            .Returns(false);
        retryPolicy.Setup(instance => instance.TryGetDelay(
                1,
                null,
                It.IsAny<HttpRequestException>(),
                out It.Ref<TimeSpan>.IsAny))
            .Returns(false);
        var transport = CreateTransport(http, retryPolicy.Object);

        Func<Task> act = () => transport.GetAsync<TestDto>(
            new Uri("https://api.bitbucket.org/2.0/user"),
            cancellation.Token);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*400*Bad Request*invalid*");
        retryPolicy.VerifyAll();
    }

    [Fact(DisplayName = "Get async retries transient response")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenResponseIsRetryableRetriesAndReturnsDto()
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new SequenceHttpMessageHandler(
            CreateResponse(HttpStatusCode.InternalServerError, "failed"),
            CreateResponse(HttpStatusCode.OK, """{"Value":"result"}"""));
        using var http = new HttpClient(handler);
        var delay = TimeSpan.Zero;
        var retryPolicy = new Mock<IBitbucketRetryPolicy>(MockBehavior.Strict);
        retryPolicy.Setup(instance => instance.TryGetDelay(
                1,
                HttpStatusCode.InternalServerError,
                null,
                out delay))
            .Returns(true);
        var transport = CreateTransport(http, retryPolicy.Object);

        var result = await transport.GetAsync<TestDto>(
            new Uri("https://api.bitbucket.org/2.0/user"),
            cancellation.Token);

        result.Should().Be(new TestDto("result"));
        handler.RequestTokens.Should().HaveCount(2);
        retryPolicy.VerifyAll();
    }

    [Fact(DisplayName = "Get async retries HTTP request exception")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenHttpRequestFailsRetriesAndReturnsDto()
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new SequenceHttpMessageHandler(
            new HttpRequestException("network"),
            CreateResponse(HttpStatusCode.OK, """{"Value":"result"}"""));
        using var http = new HttpClient(handler);
        var delay = TimeSpan.Zero;
        var retryPolicy = new Mock<IBitbucketRetryPolicy>(MockBehavior.Strict);
        retryPolicy.Setup(instance => instance.TryGetDelay(
                1,
                null,
                It.Is<HttpRequestException>(exception => exception.Message == "network"),
                out delay))
            .Returns(true);
        var transport = CreateTransport(http, retryPolicy.Object);

        var result = await transport.GetAsync<TestDto>(
            new Uri("https://api.bitbucket.org/2.0/user"),
            cancellation.Token);

        result.Should().Be(new TestDto("result"));
        retryPolicy.VerifyAll();
    }

    [Fact(DisplayName = "Get async observes cancellation")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenCancelledThrowsOperationCanceledException()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var handler = new SequenceHttpMessageHandler(CreateResponse(HttpStatusCode.OK, "{}"));
        using var http = new HttpClient(handler);
        var transport = CreateTransport(http);

        Func<Task> act = () => transport.GetAsync<TestDto>(
            new Uri("https://api.bitbucket.org/2.0/user"),
            cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static BitbucketTransport CreateTransport(
        HttpClient http,
        IBitbucketRetryPolicy? retryPolicy = null,
        IBitbucketTelemetryService? telemetryService = null) =>
        new(
            http,
            retryPolicy ?? Mock.Of<IBitbucketRetryPolicy>(),
            telemetryService ?? Mock.Of<IBitbucketTelemetryService>());

    private static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode,
        string content,
        string? reasonPhrase = null) =>
        new(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
            ReasonPhrase = reasonPhrase
        };

    private sealed record TestDto(string Value);

    private sealed class SequenceHttpMessageHandler(params object[] results) : HttpMessageHandler
    {
        private readonly Queue<object> _results = new(results);

        public List<CancellationToken> RequestTokens { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestTokens.Add(cancellationToken);

            var result = _results.Dequeue();
            return result switch
            {
                HttpResponseMessage response => Task.FromResult(response),
                Exception exception => Task.FromException<HttpResponseMessage>(exception),
                _ => throw new InvalidOperationException()
            };
        }
    }
}
