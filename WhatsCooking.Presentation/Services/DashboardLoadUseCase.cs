using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

namespace WhatsCooking.Services;

/// <summary>
/// Coordinates loading a complete dashboard snapshot.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Service is created by dependency injection.")]
internal sealed class DashboardLoadUseCase : IDashboardLoadUseCase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardLoadUseCase"/> class.
    /// </summary>
    public DashboardLoadUseCase(
        IPullRequestDashboardLoader loader,
        IDemoPullRequestDashboardProvider demoDataProvider,
        IDemoTelemetryProvider demoTelemetryProvider,
        IBitbucketTelemetryService telemetryService,
        TimeProvider timeProvider,
        IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(demoDataProvider);
        ArgumentNullException.ThrowIfNull(demoTelemetryProvider);
        ArgumentNullException.ThrowIfNull(telemetryService);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);

        _loader = loader;
        _demoDataProvider = demoDataProvider;
        _demoTelemetryProvider = demoTelemetryProvider;
        _telemetryService = telemetryService;
        _timeProvider = timeProvider;
        _isDemoMode = options.Value.DemoMode;
    }

    /// <inheritdoc />
    public async Task<DashboardLoadResult> LoadAsync(
        FilterPattern filterPattern,
        int mergedPullRequestsDays,
        IProgress<PullRequestLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await LoadSnapshotAsync(
                filterPattern,
                mergedPullRequestsDays,
                progress,
                cancellationToken).ConfigureAwait(false);
            return new DashboardLoadResult.Success(snapshot);
        }
        catch (OperationCanceledException)
        {
            return new DashboardLoadResult.Cancelled();
        }
        catch (HttpRequestException ex)
        {
            return new DashboardLoadResult.Failure(ex.Message);
        }
        catch (JsonException ex)
        {
            return new DashboardLoadResult.Failure(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return new DashboardLoadResult.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return new DashboardLoadResult.Failure(ex.Message);
        }
    }

    private async Task<PullRequestDashboardSnapshot> LoadSnapshotAsync(
        FilterPattern filterPattern,
        int mergedPullRequestsDays,
        IProgress<PullRequestLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var asOf = _timeProvider.GetLocalNow();
        if (_isDemoMode)
        {
            progress?.Report(new PullRequestLoadProgress("Loading demo data"));
            var demoResult = _demoDataProvider.Create();
            return new PullRequestDashboardSnapshot(
                asOf,
                demoResult.Repositories,
                demoResult.OpenPullRequests,
                demoResult.MergedPullRequests,
                _demoTelemetryProvider.Create());
        }

        var result = await _loader
            .LoadAsync(filterPattern, mergedPullRequestsDays, progress, cancellationToken)
            .ConfigureAwait(false);

        return new PullRequestDashboardSnapshot(
            asOf,
            result.Repositories,
            result.OpenPullRequests,
            result.MergedPullRequests,
            _telemetryService.GetSnapshot());
    }

    private readonly IPullRequestDashboardLoader _loader;

    private readonly IDemoPullRequestDashboardProvider _demoDataProvider;

    private readonly IDemoTelemetryProvider _demoTelemetryProvider;

    private readonly IBitbucketTelemetryService _telemetryService;

    private readonly TimeProvider _timeProvider;

    private readonly bool _isDemoMode;
}
