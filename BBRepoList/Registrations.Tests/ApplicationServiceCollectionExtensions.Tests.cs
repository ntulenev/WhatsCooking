using System.Net;
using System.Net.Http.Headers;
using System.Text;

using BBRepoList.Abstractions;
using BBRepoList.API;
using BBRepoList.API.Helpers;
using BBRepoList.Caching;
using BBRepoList.Configuration;
using BBRepoList.Logic;
using BBRepoList.Telemetry;
using BBRepoList.Transport;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BBRepoList.Registrations.Tests;

public sealed class ApplicationServiceCollectionExtensionsTests
{
    [Fact(DisplayName = "Add application services throws when services are null")]
    [Trait("Category", "Unit")]
    public void AddApplicationServicesWhenServicesAreNullThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        Action act = () => services.AddApplicationServices(CreateConfiguration());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Add application services throws when configuration is null")]
    [Trait("Category", "Unit")]
    public void AddApplicationServicesWhenConfigurationIsNullThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration configuration = null!;

        // Act
        Action act = () => services.AddApplicationServices(configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Add application services returns the same service collection")]
    [Trait("Category", "Unit")]
    public void AddApplicationServicesWhenArgumentsAreValidReturnsSameCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddApplicationServices(CreateConfiguration());

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact(DisplayName = "Add application services binds Bitbucket options")]
    [Trait("Category", "Unit")]
    public void AddApplicationServicesWhenConfigurationIsValidBindsOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        _ = services.AddApplicationServices(CreateConfiguration());
        using var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<BitbucketOptions>>().Value;

        // Assert
        options.BaseUrl.Should().Be(new Uri("https://api.bitbucket.example/2.0"));
        options.Workspace.Should().Be("workspace");
        options.AuthEmail.Should().Be("user@example.com");
        options.AuthApiToken.Should().Be("token");
        options.PageLen.Should().Be(50);
        options.RetryCount.Should().Be(3);
        options.PullRequestDetails.TtfrThresholdHours.Should().Be(6);
        options.PullRequestDetails.MinimalDescriptionTextLength.Should().Be(10);
        options.PullRequestDetails.LoadThreshold.Should().Be(4);
        options.MergedPullRequests.LoadThreshold.Should().Be(2);
        options.Telemetry.Enabled.Should().BeTrue();
    }

    [Fact(DisplayName = "Add application services validates invalid Bitbucket options")]
    [Trait("Category", "Unit")]
    public void AddApplicationServicesWhenConfigurationIsInvalidRejectsOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        _ = services.AddApplicationServices(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();

        // Act
        Action act = () => _ = provider.GetRequiredService<IOptions<BitbucketOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }

    [Fact(DisplayName = "Add application services configures Bitbucket HTTP client")]
    [Trait("Category", "Unit")]
    public async Task AddApplicationServicesWhenConfigurationIsValidConfiguresHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        _ = services.AddApplicationServices(CreateConfiguration());
        using var cancellation = new CancellationTokenSource();
        using var handler = new RecordingHttpMessageHandler();
        _ = services
            .AddHttpClient<IBitbucketTransport, BitbucketTransport>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        using var provider = services.BuildServiceProvider();
        var transport = provider.GetRequiredService<IBitbucketTransport>();

        // Act
        _ = await transport.GetAsync<object>(new Uri("repositories", UriKind.Relative), cancellation.Token);

        // Assert
        handler.RequestUri.Should().Be(new Uri("https://api.bitbucket.example/2.0/repositories"));
        handler.AcceptMediaTypes.Should().Equal("application/json");
        handler.Authorization.Should().Be(
            new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("user@example.com:token"))));
    }

    [Fact(DisplayName = "Add application services registers expected service lifetimes")]
    [Trait("Category", "Unit")]
    public void AddApplicationServicesWhenConfigurationIsValidRegistersExpectedServiceLifetimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        _ = services.AddApplicationServices(CreateConfiguration());

        // Assert
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(TimeProvider)
            && descriptor.Lifetime == ServiceLifetime.Singleton);
        AssertRegistration<IBitbucketRetryPolicy, BitbucketRetryPolicy>(services, ServiceLifetime.Singleton);
        AssertRegistration<IBitbucketTelemetryService, BitbucketTelemetryService>(services, ServiceLifetime.Singleton);
        AssertRegistration<IBitbucketErrorResponseParser, BitbucketErrorResponseParser>(services, ServiceLifetime.Singleton);
        AssertRegistration<IPullRequestDetailsCache, FilePullRequestDetailsCache>(services, ServiceLifetime.Singleton);
        AssertRegistration<IPullRequestDetailsCacheService, PullRequestDetailsCacheService>(services, ServiceLifetime.Singleton);
        AssertRegistration<IBitbucketAuthApiClient, BitbucketAuthApiClient>(services, ServiceLifetime.Transient);
        AssertRegistration<IBitbucketRepoApiClient, BitbucketRepoApiClient>(services, ServiceLifetime.Transient);
        AssertRegistration<IBitbucketJsonParser, BitbucketJsonParser>(services, ServiceLifetime.Transient);
        AssertRegistration<IBitbucketPullRequestUrlBuilder, BitbucketPullRequestUrlBuilder>(services, ServiceLifetime.Transient);
        AssertRegistration<IBitbucketPullRequestPageReader, BitbucketPullRequestPageReader>(services, ServiceLifetime.Transient);
        AssertRegistration<IPullRequestActivitySummaryProvider, PullRequestActivitySummaryProvider>(services, ServiceLifetime.Transient);
        AssertRegistration<IPullRequestDomainFactory, PullRequestDomainFactory>(services, ServiceLifetime.Transient);
        AssertRegistration<IBitbucketOpenPullRequestService, BitbucketOpenPullRequestService>(services, ServiceLifetime.Transient);
        AssertRegistration<IBitbucketMergedPullRequestService, BitbucketMergedPullRequestService>(services, ServiceLifetime.Transient);
        AssertRegistration<IBitbucketPRApiClient, BitbucketPRApiClient>(services, ServiceLifetime.Transient);
        AssertRegistration<IPullRequestActivityAnalyzer, PullRequestActivityAnalyzer>(services, ServiceLifetime.Transient);
        AssertRegistration<IBitbucketPullRequestActivityLoader, BitbucketPullRequestActivityLoader>(services, ServiceLifetime.Transient);
        AssertRegistration<IPullRequestFingerprintBuilder, PullRequestFingerprintBuilder>(services, ServiceLifetime.Transient);
        AssertRegistration<IPullRequestSnapshotMapper, PullRequestSnapshotMapper>(services, ServiceLifetime.Transient);
        AssertRegistration<IRepoService, RepositoryService>(services, ServiceLifetime.Transient);
        AssertRegistration<IPullRequestDiffService, PullRequestDiffService>(services, ServiceLifetime.Singleton);
    }

    [Fact(DisplayName = "Add application services resolves application service graph")]
    [Trait("Category", "Unit")]
    public void AddApplicationServicesWhenConfigurationIsValidResolvesApplicationServiceGraph()
    {
        // Arrange
        var services = new ServiceCollection();
        _ = services.AddApplicationServices(CreateConfiguration());
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        // Act
        var repositoryService = provider.GetRequiredService<IRepoService>();
        var cache = provider.GetRequiredService<IPullRequestDetailsCache>();
        var cacheService = provider.GetRequiredService<IPullRequestDetailsCacheService>();
        var diffService = provider.GetRequiredService<IPullRequestDiffService>();
        var errorResponseParser = provider.GetRequiredService<IBitbucketErrorResponseParser>();
        var telemetry = provider.GetRequiredService<IBitbucketTelemetryService>();
        var timeProvider = provider.GetRequiredService<TimeProvider>();

        // Assert
        repositoryService.Should().BeOfType<RepositoryService>();
        cache.Should().BeOfType<FilePullRequestDetailsCache>();
        cacheService.Should().BeOfType<PullRequestDetailsCacheService>();
        diffService.Should().BeOfType<PullRequestDiffService>();
        errorResponseParser.Should().BeOfType<BitbucketErrorResponseParser>();
        telemetry.Should().BeOfType<BitbucketTelemetryService>();
        timeProvider.Should().BeSameAs(TimeProvider.System);
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bitbucket:BaseUrl"] = "https://api.bitbucket.example/2.0",
                ["Bitbucket:Workspace"] = "workspace",
                ["Bitbucket:AuthEmail"] = "user@example.com",
                ["Bitbucket:AuthApiToken"] = "token",
                ["Bitbucket:PageLen"] = "50",
                ["Bitbucket:RetryCount"] = "3",
                ["Bitbucket:PullRequestDetails:TtfrThresholdHours"] = "6",
                ["Bitbucket:PullRequestDetails:MinimalDescriptionTextLength"] = "10",
                ["Bitbucket:PullRequestDetails:LoadThreshold"] = "4",
                ["Bitbucket:MergedPullRequests:LoadThreshold"] = "2",
                ["Bitbucket:Telemetry:Enabled"] = "true"
            })
            .Build();

    private static void AssertRegistration<TService, TImplementation>(
        IServiceCollection services,
        ServiceLifetime lifetime)
    {
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(TService)
            && descriptor.ImplementationType == typeof(TImplementation)
            && descriptor.Lifetime == lifetime);
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        public AuthenticationHeaderValue? Authorization { get; private set; }

        public IReadOnlyList<string> AcceptMediaTypes { get; private set; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestUri = request.RequestUri;
            Authorization = request.Headers.Authorization;
            AcceptMediaTypes =
            [
                .. request.Headers.Accept
                    .Select(static header => header.MediaType)
                    .OfType<string>()
            ];

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        }
    }
}
