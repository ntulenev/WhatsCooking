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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BBRepoList.Registrations;

/// <summary>
/// Registers BBRepoList application services in the dependency injection container.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    private const string BITBUCKET_SECTION_NAME = "Bitbucket";

    /// <summary>
    /// Adds BBRepoList configuration, Bitbucket API, caching, and repository services.
    /// </summary>
    /// <param name="services">Service collection to update.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same service collection instance for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services.AddApplicationOptions(configuration);
        _ = services.AddSystemServices();
        _ = services.AddBitbucketTransport();
        _ = services.AddBitbucketApiClients();
        _ = services.AddPullRequestServices();
        _ = services.AddCaching();
        _ = services.AddApplicationWorkflow();
        return services;
    }

    private static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services
            .AddOptions<BitbucketOptions>()
            .Bind(configuration.GetSection(BITBUCKET_SECTION_NAME))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    private static IServiceCollection AddSystemServices(this IServiceCollection services)
    {
        _ = services.AddSingleton(TimeProvider.System);
        return services;
    }

    private static IServiceCollection AddApplicationWorkflow(this IServiceCollection services)
    {
        _ = services.AddTransient<IPullRequestRepositoryBatchLoader, PullRequestRepositoryBatchLoader>();
        _ = services.AddSingleton<IPullRequestResultSorter, PullRequestResultSorter>();
        _ = services.AddTransient<IRepoService, RepositoryService>();
        _ = services.AddSingleton<IPullRequestDiffService, PullRequestDiffService>();
        return services;
    }

    private static IServiceCollection AddBitbucketTransport(this IServiceCollection services)
    {
        _ = services.AddHttpClient<IBitbucketTransport, BitbucketTransport>((sp, http) =>
        {
            var settings = sp.GetRequiredService<IOptions<BitbucketOptions>>().Value;

            http.BaseAddress = new Uri(settings.BaseUrl.ToString().TrimEnd('/') + "/");
            http.DefaultRequestHeaders.Authorization = BuildAuthHeader(settings.AuthEmail, settings.AuthApiToken);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        _ = services.AddSingleton<IBitbucketRetryPolicy, BitbucketRetryPolicy>();
        _ = services.AddSingleton<IBitbucketTelemetryService, BitbucketTelemetryService>();
        _ = services.AddSingleton<IBitbucketErrorResponseParser, BitbucketErrorResponseParser>();

        return services;
    }

    private static IServiceCollection AddBitbucketApiClients(this IServiceCollection services)
    {
        _ = services.AddTransient<IBitbucketAuthApiClient, BitbucketAuthApiClient>();
        _ = services.AddTransient<IBitbucketRepoApiClient, BitbucketRepoApiClient>();
        _ = services.AddTransient<IBitbucketJsonParser, BitbucketJsonParser>();
        _ = services.AddTransient<IBitbucketPullRequestUrlBuilder, BitbucketPullRequestUrlBuilder>();
        _ = services.AddTransient<IBitbucketPullRequestPageReader, BitbucketPullRequestPageReader>();
        _ = services.AddTransient<IPullRequestActivitySummaryProvider, PullRequestActivitySummaryProvider>();
        _ = services.AddTransient<IPullRequestDomainFactory, PullRequestDomainFactory>();
        _ = services.AddTransient<IBitbucketOpenPullRequestService, BitbucketOpenPullRequestService>();
        _ = services.AddTransient<IBitbucketMergedPullRequestService, BitbucketMergedPullRequestService>();
        _ = services.AddTransient<IBitbucketPRApiClient, BitbucketPRApiClient>();
        return services;
    }

    private static IServiceCollection AddPullRequestServices(this IServiceCollection services)
    {
        _ = services.AddTransient<IPullRequestActivityAnalyzer, PullRequestActivityAnalyzer>();
        _ = services.AddTransient<IBitbucketPullRequestActivityLoader, BitbucketPullRequestActivityLoader>();
        _ = services.AddTransient<IPullRequestFingerprintBuilder, PullRequestFingerprintBuilder>();
        _ = services.AddTransient<IPullRequestSnapshotMapper, PullRequestSnapshotMapper>();
        return services;
    }

    private static IServiceCollection AddCaching(this IServiceCollection services)
    {
        _ = services.AddSingleton<IPullRequestDetailsCache, FilePullRequestDetailsCache>();
        _ = services.AddSingleton<IPullRequestDetailsCacheService, PullRequestDetailsCacheService>();
        return services;
    }

    private static AuthenticationHeaderValue BuildAuthHeader(string authEmail, string authApiToken)
    {
        var raw = $"{authEmail}:{authApiToken}";
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));

        return new AuthenticationHeaderValue("Basic", b64);
    }
}
