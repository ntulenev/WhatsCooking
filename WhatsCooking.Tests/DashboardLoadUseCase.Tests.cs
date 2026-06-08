using System.Text.Json;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

using WhatsCooking.Services;

namespace WhatsCooking.Tests;

public sealed class DashboardLoadUseCaseTests
{
    [Fact(DisplayName = "Constructor throws when loader is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenLoaderIsNullThrowsArgumentNullException()
    {
        // Arrange
        IPullRequestDashboardLoader loader = null!;

        // Act
        Action act = () => _ = new DashboardLoadUseCase(
            loader,
            new Mock<IDemoPullRequestDashboardProvider>(MockBehavior.Strict).Object,
            new Mock<IDemoTelemetryProvider>(MockBehavior.Strict).Object,
            new Mock<IBitbucketTelemetryService>(MockBehavior.Strict).Object,
            TimeProvider.System,
            CreateOptions());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when demo data provider is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenDemoDataProviderIsNullThrowsArgumentNullException()
    {
        // Arrange
        IDemoPullRequestDashboardProvider demoDataProvider = null!;

        // Act
        Action act = () => _ = new DashboardLoadUseCase(
            new Mock<IPullRequestDashboardLoader>(MockBehavior.Strict).Object,
            demoDataProvider,
            new Mock<IDemoTelemetryProvider>(MockBehavior.Strict).Object,
            new Mock<IBitbucketTelemetryService>(MockBehavior.Strict).Object,
            TimeProvider.System,
            CreateOptions());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when demo telemetry provider is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenDemoTelemetryProviderIsNullThrowsArgumentNullException()
    {
        // Arrange
        IDemoTelemetryProvider demoTelemetryProvider = null!;

        // Act
        Action act = () => _ = new DashboardLoadUseCase(
            new Mock<IPullRequestDashboardLoader>(MockBehavior.Strict).Object,
            new Mock<IDemoPullRequestDashboardProvider>(MockBehavior.Strict).Object,
            demoTelemetryProvider,
            new Mock<IBitbucketTelemetryService>(MockBehavior.Strict).Object,
            TimeProvider.System,
            CreateOptions());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when telemetry service is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTelemetryServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketTelemetryService telemetryService = null!;

        // Act
        Action act = () => _ = new DashboardLoadUseCase(
            new Mock<IPullRequestDashboardLoader>(MockBehavior.Strict).Object,
            new Mock<IDemoPullRequestDashboardProvider>(MockBehavior.Strict).Object,
            new Mock<IDemoTelemetryProvider>(MockBehavior.Strict).Object,
            telemetryService,
            TimeProvider.System,
            CreateOptions());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when time provider is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTimeProviderIsNullThrowsArgumentNullException()
    {
        // Arrange
        TimeProvider timeProvider = null!;

        // Act
        Action act = () => _ = new DashboardLoadUseCase(
            new Mock<IPullRequestDashboardLoader>(MockBehavior.Strict).Object,
            new Mock<IDemoPullRequestDashboardProvider>(MockBehavior.Strict).Object,
            new Mock<IDemoTelemetryProvider>(MockBehavior.Strict).Object,
            new Mock<IBitbucketTelemetryService>(MockBehavior.Strict).Object,
            timeProvider,
            CreateOptions());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new DashboardLoadUseCase(
            new Mock<IPullRequestDashboardLoader>(MockBehavior.Strict).Object,
            new Mock<IDemoPullRequestDashboardProvider>(MockBehavior.Strict).Object,
            new Mock<IDemoTelemetryProvider>(MockBehavior.Strict).Object,
            new Mock<IBitbucketTelemetryService>(MockBehavior.Strict).Object,
            TimeProvider.System,
            options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "LoadAsync returns live dashboard snapshot")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenDemoModeIsDisabledReturnsLiveDashboardSnapshot()
    {
        // Arrange
        var asOf = new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
        var filterPattern = new FilterPattern("api", RepositorySearchMode.Contains);
        using var cts = new CancellationTokenSource();
        var repositories = new[] { new Repository("API", slug: new RepositorySlug("api")) };
        var loadResult = new PullRequestLoadResult(repositories, [], []);
        var telemetry = new BitbucketTelemetrySnapshot(true, 2, [new BitbucketApiRequestStatistic("user", 2)]);
        var loaderCalls = 0;
        var telemetryCalls = 0;

        var loader = new Mock<IPullRequestDashboardLoader>(MockBehavior.Strict);
        loader.Setup(instance => instance.LoadAsync(
                filterPattern,
                30,
                It.Is<IProgress<PullRequestLoadProgress>?>(progress => progress != null),
                cts.Token))
            .Callback(() => loaderCalls++)
            .ReturnsAsync(loadResult);
        var telemetryService = new Mock<IBitbucketTelemetryService>(MockBehavior.Strict);
        telemetryService.Setup(instance => instance.GetSnapshot())
            .Callback(() => telemetryCalls++)
            .Returns(telemetry);
        var useCase = CreateUseCase(
            loader: loader.Object,
            telemetryService: telemetryService.Object,
            timeProvider: new FixedTimeProvider(asOf));

        // Act
        var result = await useCase.LoadAsync(
            filterPattern,
            30,
            new RecordingProgress<PullRequestLoadProgress>(),
            cts.Token);

        // Assert
        result.Should().BeOfType<DashboardLoadResult.Success>()
            .Which.Snapshot.Should().BeEquivalentTo(new
            {
                AsOf = asOf,
                Repositories = repositories,
                OpenPullRequests = Array.Empty<PullRequestDetail>(),
                MergedPullRequests = Array.Empty<MergedPullRequest>(),
                Telemetry = telemetry
            });
        loaderCalls.Should().Be(1);
        telemetryCalls.Should().Be(1);
        loader.VerifyAll();
        telemetryService.VerifyAll();
    }

    [Fact(DisplayName = "LoadAsync returns demo dashboard snapshot")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenDemoModeIsEnabledReturnsDemoDashboardSnapshot()
    {
        // Arrange
        var asOf = new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
        var demoResult = new PullRequestLoadResult([new Repository("Demo")], [], []);
        var telemetry = new BitbucketTelemetrySnapshot(true, 1, []);
        var demoDataCalls = 0;
        var demoTelemetryCalls = 0;
        var progress = new RecordingProgress<PullRequestLoadProgress>();
        var demoDataProvider = new Mock<IDemoPullRequestDashboardProvider>(MockBehavior.Strict);
        demoDataProvider.Setup(instance => instance.Create())
            .Callback(() => demoDataCalls++)
            .Returns(demoResult);
        var demoTelemetryProvider = new Mock<IDemoTelemetryProvider>(MockBehavior.Strict);
        demoTelemetryProvider.Setup(instance => instance.Create())
            .Callback(() => demoTelemetryCalls++)
            .Returns(telemetry);
        var useCase = CreateUseCase(
            demoDataProvider: demoDataProvider.Object,
            demoTelemetryProvider: demoTelemetryProvider.Object,
            timeProvider: new FixedTimeProvider(asOf),
            options: Options.Create(new BitbucketOptions
            {
                BaseUrl = new Uri("https://api.bitbucket.org/2.0/"),
                DemoMode = true
            }));

        // Act
        var result = await useCase.LoadAsync(
            new FilterPattern(null),
            1,
            progress,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<DashboardLoadResult.Success>()
            .Which.Snapshot.AsOf.Should().Be(asOf);
        progress.Values.Should().Equal(new PullRequestLoadProgress(PullRequestLoadStage.LoadingDemoData));
        demoDataCalls.Should().Be(1);
        demoTelemetryCalls.Should().Be(1);
        demoDataProvider.VerifyAll();
        demoTelemetryProvider.VerifyAll();
    }

    [Theory(DisplayName = "LoadAsync converts expected exceptions to failure")]
    [Trait("Category", "Unit")]
    [InlineData(0, "HTTP failed")]
    [InlineData(1, "JSON failed")]
    [InlineData(2, "Operation failed")]
    [InlineData(3, "Argument failed")]
    public async Task LoadAsyncWhenExpectedExceptionOccursReturnsFailure(int exceptionKind, string message)
    {
        // Arrange
        Exception exception = exceptionKind switch
        {
            0 => new HttpRequestException(message),
            1 => new JsonException(message),
            2 => new InvalidOperationException(message),
            _ => new ArgumentException(message)
        };
        var loader = new Mock<IPullRequestDashboardLoader>(MockBehavior.Strict);
        loader.Setup(instance => instance.LoadAsync(
                It.Is<FilterPattern>(filter => filter == new FilterPattern(null)),
                1,
                null,
                CancellationToken.None))
            .ThrowsAsync(exception);
        var useCase = CreateUseCase(loader: loader.Object);

        // Act
        var result = await useCase.LoadAsync(
            new FilterPattern(null),
            1,
            progress: null,
            CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(new DashboardLoadResult.Failure(message));
        loader.VerifyAll();
    }

    [Fact(DisplayName = "LoadAsync returns cancelled result when loading is cancelled")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenOperationIsCancelledReturnsCancelled()
    {
        // Arrange
        var loader = new Mock<IPullRequestDashboardLoader>(MockBehavior.Strict);
        loader.Setup(instance => instance.LoadAsync(
                It.Is<FilterPattern>(filter => filter == new FilterPattern(null)),
                1,
                null,
                CancellationToken.None))
            .ThrowsAsync(new OperationCanceledException());
        var useCase = CreateUseCase(loader: loader.Object);

        // Act
        var result = await useCase.LoadAsync(
            new FilterPattern(null),
            1,
            progress: null,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<DashboardLoadResult.Cancelled>();
        loader.VerifyAll();
    }

    private static DashboardLoadUseCase CreateUseCase(
        IPullRequestDashboardLoader? loader = null,
        IDemoPullRequestDashboardProvider? demoDataProvider = null,
        IDemoTelemetryProvider? demoTelemetryProvider = null,
        IBitbucketTelemetryService? telemetryService = null,
        TimeProvider? timeProvider = null,
        IOptions<BitbucketOptions>? options = null) =>
        new(
            loader ?? new Mock<IPullRequestDashboardLoader>(MockBehavior.Strict).Object,
            demoDataProvider ?? new Mock<IDemoPullRequestDashboardProvider>(MockBehavior.Strict).Object,
            demoTelemetryProvider ?? new Mock<IDemoTelemetryProvider>(MockBehavior.Strict).Object,
            telemetryService ?? new Mock<IBitbucketTelemetryService>(MockBehavior.Strict).Object,
            timeProvider ?? new FixedTimeProvider(new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero)),
            options ?? CreateOptions());

    private static IOptions<BitbucketOptions> CreateOptions() =>
        Options.Create(new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/")
        });
}
