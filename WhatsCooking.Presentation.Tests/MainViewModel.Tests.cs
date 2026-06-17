using System.Collections.Specialized;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

using WhatsCooking.Services;
using WhatsCooking.ViewModels;

namespace WhatsCooking.Presentation.Tests;

public sealed class MainViewModelTests
{
    [Theory(DisplayName = "Constructor throws when required dependency is null")]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void ConstructorWhenRequiredDependencyIsNullThrowsArgumentNullException(int dependencyIndex)
    {
        // Arrange
        var loadUseCase = new Mock<IDashboardLoadUseCase>(MockBehavior.Strict).Object;
        var telemetryViewModel = new TelemetryViewModel(
            new Mock<IBitbucketTelemetryService>(MockBehavior.Strict).Object,
            Mock.Of<IPullRequestDetailsCache>(instance => instance.GetSizeInBytes() == 0),
            Mock.Of<IDialogService>(),
            new Mock<IDebouncer>(MockBehavior.Strict).Object);
        var rowMapper = CreateRowMapper();
        var dialogService = new Mock<IDialogService>(MockBehavior.Strict).Object;
        var externalUrlLauncher = new Mock<IExternalUrlLauncher>(MockBehavior.Strict).Object;
        var aiReviewPromptService = new Mock<IAiReviewPromptService>(MockBehavior.Strict).Object;
        var preferencesService = new Mock<IUserPreferencesService>(MockBehavior.Strict).Object;

        // Act
        Action act = () => _ = new MainViewModel(
            dependencyIndex == 0 ? null! : loadUseCase,
            dependencyIndex == 1 ? null! : telemetryViewModel,
            dependencyIndex == 2 ? null! : rowMapper,
            dependencyIndex == 3 ? null! : dialogService,
            dependencyIndex == 4 ? null! : externalUrlLauncher,
            dependencyIndex == 5 ? null! : aiReviewPromptService,
            dependencyIndex == 6 ? null! : preferencesService);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor restores normalized preferences and telemetry")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPreferencesExistRestoresNormalizedState()
    {
        // Arrange
        var preferences = new UserPreferences
        {
            IsLightTheme = true,
            SearchPhrase = "payments",
            SearchMode = RepositorySearchMode.Contains,
            UiScale = 2
        };
        var fixture = CreateFixture(preferences);

        // Act
        using var viewModel = fixture.CreateViewModel();

        // Assert
        viewModel.IsLightTheme.Should().BeTrue();
        viewModel.SearchPhrase.Should().Be("payments");
        viewModel.SelectedSearchMode.Should().Be(RepositorySearchMode.Contains);
        viewModel.UiScale.Should().Be(1.5);
        viewModel.Status.Should().Be("Ready");
        viewModel.TelemetryDashboard.TelemetryRequestsCount.Should().Be(0);
        MainViewModel.SearchModes.Cast<RepositorySearchMode>().Should().BeEquivalentTo(Enum.GetValues<RepositorySearchMode>());
        fixture.PreferencesService.VerifyAll();
        fixture.TelemetryService.VerifyAll();
    }

    [Fact(DisplayName = "Merged pull request period validation updates errors and command state")]
    [Trait("Category", "Unit")]
    public void MergedPullRequestsDaysInputWhenInvalidThenValidUpdatesValidationState()
    {
        // Arrange
        var fixture = CreateFixture();
        using var viewModel = fixture.CreateViewModel();
        var changedProperties = new List<string?>();
        viewModel.ErrorsChanged += (_, args) => changedProperties.Add(args.PropertyName);

        // Act
        viewModel.MergedPullRequestsDaysInput = "0";
        var errors = viewModel.GetErrors(nameof(MainViewModel.MergedPullRequestsDaysInput)).Cast<string>().ToArray();
        var canLoadWhenInvalid = viewModel.LoadCommand.CanExecute(null);
        viewModel.MergedPullRequestsDaysInput = "30";

        // Assert
        errors.Should().Equal("Enter a whole number from 1 to 365.");
        canLoadWhenInvalid.Should().BeFalse();
        viewModel.HasErrors.Should().BeFalse();
        viewModel.MergedPullRequestsDays.Should().Be(30);
        viewModel.LoadCommand.CanExecute(null).Should().BeTrue();
        changedProperties.Should().Equal(
            nameof(MainViewModel.MergedPullRequestsDaysInput),
            nameof(MainViewModel.MergedPullRequestsDaysInput));
        viewModel.GetErrors(null).Cast<string>().Should().BeEmpty();
    }

    [Fact(DisplayName = "Merged filter properties delegate to filter state")]
    [Trait("Category", "Unit")]
    public void MergedFilterPropertiesWhenSetDelegateToFilterState()
    {
        // Arrange
        var fixture = CreateFixture();
        using var viewModel = fixture.CreateViewModel();

        // Act
        viewModel.MergedNumberFilter = "1";
        viewModel.MergedRepositoryFilter = "repo";
        viewModel.MergedPullRequestFilter = "title";
        viewModel.MergedAuthorFilter = "author";
        viewModel.MergedDescriptionLengthFilter = "10";
        viewModel.MergedTimeToFirstResponseFilter = "1h";
        viewModel.MergedActivityFilter = "2h";
        viewModel.MergedCommentsFilter = "3";
        viewModel.MergedRequestChangesFilter = "RC";
        viewModel.MergedApprovalsFilter = "AP";
        viewModel.MergedCurrentUserActivityFilter = "Comment";

        // Assert
        viewModel.MergedPullRequestFilters.Should().BeEquivalentTo(new
        {
            Number = "1",
            Repository = "repo",
            PullRequest = "title",
            Author = "author",
            DescriptionLength = "10",
            TimeToFirstResponse = "1h",
            Activity = "2h",
            Comments = "3",
            RequestChanges = "RC",
            Approvals = "AP",
            CurrentUserActivity = "Comment"
        });
    }

    [Fact(DisplayName = "Load command applies successful dashboard snapshot")]
    [Trait("Category", "Unit")]
    public async Task LoadCommandWhenUseCaseSucceedsAppliesDashboardSnapshot()
    {
        // Arrange
        var asOf = new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
        var repository = new Repository("Payments", slug: new RepositorySlug("payments"));
        var openPullRequest = new PullRequestDetail(
            repository,
            new PullRequestId(10),
            "Open title",
            asOf.AddHours(-2),
            null,
            "Nikita",
            asOf.AddHours(-1),
            asOf.AddMinutes(-30),
            true);
        var mergedPullRequest = new MergedPullRequest(
            repository,
            new PullRequestId(9),
            "Merged title",
            asOf.AddDays(-2),
            null,
            "Nikita",
            asOf.AddDays(-1),
            asOf.AddHours(-3),
            false,
            asOf.AddHours(-2));
        var snapshot = new PullRequestDashboardSnapshot(
            asOf,
            [repository],
            [openPullRequest],
            [mergedPullRequest],
            new BitbucketTelemetrySnapshot(true, 3, [new BitbucketApiRequestStatistic("user", 3)]));
        var fixture = CreateFixture();
        var loadCalls = 0;
        var saveCalls = 0;
        fixture.LoadUseCase.Setup(instance => instance.LoadAsync(
                new FilterPattern("pay", RepositorySearchMode.Contains),
                7,
                It.IsAny<IProgress<PullRequestLoadProgress>?>(),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .Callback(() => loadCalls++)
            .ReturnsAsync(new DashboardLoadResult.Success(snapshot));
        fixture.PreferencesService.Setup(instance => instance.Save(
                It.Is<UserPreferences>(preferences =>
                    preferences.SearchPhrase == "pay"
                    && preferences.SearchMode == RepositorySearchMode.Contains)))
            .Callback(() => saveCalls++);
        using var viewModel = fixture.CreateViewModel();
        viewModel.SearchPhrase = "pay";
        viewModel.SelectedSearchMode = RepositorySearchMode.Contains;
        viewModel.MergedPullRequestsDaysInput = "7";

        // Act
        await viewModel.LoadCommand.ExecuteAsync();

        // Assert
        viewModel.IsLoading.Should().BeFalse();
        viewModel.RepositoriesCount.Should().Be(1);
        viewModel.OpenPullRequestsCount.Should().Be(1);
        viewModel.MergedPullRequestsCount.Should().Be(1);
        viewModel.OpenPullRequestsView.Should().ContainSingle()
            .Which.Title.Should().Be("Open title");
        viewModel.MergedPullRequestsView.Should().ContainSingle()
            .Which.Title.Should().Be("Merged title");
        viewModel.Status.Should().Be("Loaded 1 open PRs and 1 merged PRs");
        viewModel.TelemetryDashboard.TelemetryRequestsCount.Should().Be(3);
        fixture.AiReviewPromptService.Setup(instance => instance.CopyPrompt(
            It.Is<PullRequestRow>(row => row.PullRequestId == openPullRequest.PullRequestId.Value)));

        viewModel.CopyForAiCommand.Execute(viewModel.OpenPullRequestsView.Single());

        viewModel.Status.Should().Be("Copied AI review prompt for Payments #10");

        var reviewedRow = viewModel.OpenPullRequestsView.Single();
        var openViewChanges = new List<NotifyCollectionChangedAction>();
        var mergedViewChanges = new List<NotifyCollectionChangedAction>();
        viewModel.OpenPullRequestsView.CollectionChanged += (_, args) => openViewChanges.Add(args.Action);
        viewModel.MergedPullRequestsView.CollectionChanged += (_, args) => mergedViewChanges.Add(args.Action);

        reviewedRow.IsReviewed = true;
        viewModel.OpenPullRequestsView.Should().ContainSingle();
        openViewChanges.Should().BeEmpty();
        mergedViewChanges.Should().BeEmpty();

        viewModel.ToggleOpenReviewedFilterCommand.Execute(null);
        viewModel.OpenPullRequestsView.Should().BeEmpty();
        viewModel.OpenReviewedFilterButtonText.Should().Be("Show all");
        viewModel.IsOpenReviewedFilterActive.Should().BeTrue();
        openViewChanges.Should().ContainSingle()
            .Which.Should().Be(NotifyCollectionChangedAction.Reset);

        viewModel.ToggleOpenReviewedFilterCommand.Execute(null);
        viewModel.OpenReviewedFilterButtonText.Should().Be("Hide reviewed");
        viewModel.IsOpenReviewedFilterActive.Should().BeFalse();
        openViewChanges.Clear();
        reviewedRow.IsReviewed = false;
        viewModel.OpenPullRequestsView.Should().ContainSingle();
        openViewChanges.Should().BeEmpty();

        viewModel.ToggleOpenReviewedFilterCommand.Execute(null);
        openViewChanges.Clear();
        reviewedRow.IsReviewed = true;
        viewModel.OpenPullRequestsView.Should().BeEmpty();
        openViewChanges.Should().ContainSingle()
            .Which.Should().Be(NotifyCollectionChangedAction.Remove);
        mergedViewChanges.Should().BeEmpty();

        loadCalls.Should().Be(1);
        saveCalls.Should().Be(1);
        fixture.LoadUseCase.VerifyAll();
        fixture.AiReviewPromptService.VerifyAll();
        fixture.PreferencesService.VerifyAll();
    }

    [Fact(DisplayName = "Load command shows use case failure")]
    [Trait("Category", "Unit")]
    public async Task LoadCommandWhenUseCaseFailsShowsFailure()
    {
        // Arrange
        var fixture = CreateFixture();
        var dialogCalls = 0;
        fixture.LoadUseCase.Setup(instance => instance.LoadAsync(
                new FilterPattern(string.Empty, RepositorySearchMode.StartWith),
                1,
                It.IsAny<IProgress<PullRequestLoadProgress>?>(),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync(new DashboardLoadResult.Failure("Could not load"));
        fixture.PreferencesService.Setup(instance => instance.Save(
            It.Is<UserPreferences>(preferences =>
                preferences.SearchPhrase == string.Empty
                && preferences.SearchMode == RepositorySearchMode.StartWith)));
        fixture.DialogService.Setup(instance => instance.ShowLoadError("Could not load"))
            .Callback(() => dialogCalls++);
        using var viewModel = fixture.CreateViewModel();

        // Act
        await viewModel.LoadCommand.ExecuteAsync();

        // Assert
        viewModel.Status.Should().Be("Could not load");
        viewModel.IsLoading.Should().BeFalse();
        dialogCalls.Should().Be(1);
        fixture.DialogService.VerifyAll();
    }

    [Fact(DisplayName = "Load command reports cancellation")]
    [Trait("Category", "Unit")]
    public async Task LoadCommandWhenUseCaseIsCancelledReportsCancellation()
    {
        // Arrange
        var fixture = CreateFixture();
        fixture.LoadUseCase.Setup(instance => instance.LoadAsync(
                new FilterPattern(string.Empty, RepositorySearchMode.StartWith),
                1,
                It.IsAny<IProgress<PullRequestLoadProgress>?>(),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync(new DashboardLoadResult.Cancelled());
        fixture.PreferencesService.Setup(instance => instance.Save(
            It.Is<UserPreferences>(preferences =>
                preferences.SearchPhrase == string.Empty
                && preferences.SearchMode == RepositorySearchMode.StartWith)));
        using var viewModel = fixture.CreateViewModel();

        // Act
        await viewModel.LoadCommand.ExecuteAsync();

        // Assert
        viewModel.Status.Should().Be("Cancelled");
        viewModel.IsLoading.Should().BeFalse();
        fixture.LoadUseCase.VerifyAll();
    }

    [Fact(DisplayName = "UI commands update preferences, filters and external URL")]
    [Trait("Category", "Unit")]
    public void UiCommandsWhenExecutedUpdateExpectedCollaborators()
    {
        // Arrange
        var fixture = CreateFixture();
        var savedThemes = new List<bool>();
        var savedScales = new List<double?>();
        var openedUrls = new List<Uri>();
        fixture.PreferencesService.Setup(instance => instance.Save(
                It.Is<UserPreferences>(preferences =>
                    preferences.IsLightTheme
                    && preferences.UiScale == null)))
            .Callback<UserPreferences>(preferences => savedThemes.Add(preferences.IsLightTheme));
        fixture.PreferencesService.Setup(instance => instance.Save(
                It.Is<UserPreferences>(preferences =>
                    preferences.IsLightTheme
                    && (preferences.UiScale == 1.05 || preferences.UiScale == 1.0))))
            .Callback<UserPreferences>(preferences => savedScales.Add(preferences.UiScale));
        var expectedUrl = new Uri("https://bitbucket.org/platform/repo");
        fixture.ExternalUrlLauncher.Setup(instance => instance.Open(expectedUrl))
            .Callback<Uri>(url => openedUrls.Add(url));
        using var viewModel = fixture.CreateViewModel();
        viewModel.GlobalSearch = "filter";
        viewModel.MergedAuthorFilter = "author";
        viewModel.ToggleMergedReviewedFilterCommand.Execute(null);
        viewModel.MergedPullRequestFilters.HideReviewed.Should().BeTrue();
        viewModel.TelemetryDashboard.TelemetryFilter = string.Empty;

        // Act
        viewModel.IsLightTheme = true;
        viewModel.IncreaseUiScaleCommand.Execute(null);
        viewModel.DecreaseUiScaleCommand.Execute(null);
        viewModel.IncreaseUiScaleCommand.Execute(null);
        viewModel.OpenUrlCommand.Execute("not a URI");
        viewModel.OpenUrlCommand.Execute(expectedUrl);
        viewModel.ResetFiltersCommand.Execute(null);

        // Assert
        viewModel.UiScale.Should().Be(1.05);
        viewModel.GlobalSearch.Should().BeEmpty();
        viewModel.MergedAuthorFilter.Should().BeEmpty();
        viewModel.MergedPullRequestFilters.HideReviewed.Should().BeFalse();
        savedThemes.Should().Equal(true);
        savedScales.Should().Equal(1.05, 1.0, 1.05);
        openedUrls.Should().Equal(expectedUrl);
        fixture.PreferencesService.VerifyAll();
        fixture.ExternalUrlLauncher.VerifyAll();
    }

    private static MainViewModelFixture CreateFixture(UserPreferences? preferences = null)
    {
        var emptyTelemetry = new BitbucketTelemetrySnapshot(false, 0, []);
        var loadUseCase = new Mock<IDashboardLoadUseCase>(MockBehavior.Strict);
        var telemetryService = new Mock<IBitbucketTelemetryService>(MockBehavior.Strict);
        telemetryService.Setup(instance => instance.GetSnapshot()).Returns(emptyTelemetry);
        var cache = new Mock<IPullRequestDetailsCache>(MockBehavior.Strict);
        cache.Setup(instance => instance.GetSizeInBytes()).Returns(0);
        var telemetryDebouncer = new Mock<IDebouncer>(MockBehavior.Strict);
        var dialogService = new Mock<IDialogService>(MockBehavior.Strict);
        var externalUrlLauncher = new Mock<IExternalUrlLauncher>(MockBehavior.Strict);
        var aiReviewPromptService = new Mock<IAiReviewPromptService>(MockBehavior.Strict);
        var preferencesService = new Mock<IUserPreferencesService>(MockBehavior.Strict);
        preferencesService.Setup(instance => instance.Load())
            .Returns(preferences ?? new UserPreferences());

        return new MainViewModelFixture(
            loadUseCase,
            telemetryService,
            cache,
            telemetryDebouncer,
            dialogService,
            externalUrlLauncher,
            aiReviewPromptService,
            preferencesService);
    }

    private static PullRequestRowMapper CreateRowMapper() =>
        new(Options.Create(new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/"),
            Workspace = "platform",
            PullRequestDetails = new PullRequestDetailsOptions
            {
                MinimalDescriptionTextLength = 10,
                TtfrThresholdHours = 4
            }
        }));

    private sealed record MainViewModelFixture(
        Mock<IDashboardLoadUseCase> LoadUseCase,
        Mock<IBitbucketTelemetryService> TelemetryService,
        Mock<IPullRequestDetailsCache> Cache,
        Mock<IDebouncer> TelemetryDebouncer,
        Mock<IDialogService> DialogService,
        Mock<IExternalUrlLauncher> ExternalUrlLauncher,
        Mock<IAiReviewPromptService> AiReviewPromptService,
        Mock<IUserPreferencesService> PreferencesService)
    {
        public MainViewModel CreateViewModel() =>
            new(
                LoadUseCase.Object,
                new TelemetryViewModel(TelemetryService.Object, Cache.Object, DialogService.Object, TelemetryDebouncer.Object),
                CreateRowMapper(),
                DialogService.Object,
                ExternalUrlLauncher.Object,
                AiReviewPromptService.Object,
                PreferencesService.Object);
    }
}
