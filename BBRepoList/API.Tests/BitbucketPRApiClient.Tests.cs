using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Transport;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace BBRepoList.API.Tests;

public sealed class BitbucketPRApiClientTests
{
    [Fact(DisplayName = "Constructor throws when transport is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTransportIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketTransport transport = null!;

        // Act
        Action act = () => _ = new BitbucketPRApiClient(
            transport,
            Mock.Of<IPullRequestActivityAnalyzer>(),
            Mock.Of<IBitbucketPullRequestActivityLoader>(),
            Mock.Of<IPullRequestSnapshotMapper>(),
            Mock.Of<IPullRequestDetailsCacheService>(),
            CreateOptions());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Populate count skips repository without slug")]
    [Trait("Category", "Unit")]
    public async Task PopulateOpenPullRequestCountWhenRepositoryHasNoSlugSkipsTransport()
    {
        // Arrange
        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        var client = CreateClient(transport: transport);
        var repository = new Repository("Repository");

        // Act
        await client.PopulateOpenPullRequestCountAsync(repository, CancellationToken.None);

        // Assert
        repository.OpenPullRequestsCount.Should().Be(0);
        transport.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Populate count requests summary and updates repository")]
    [Trait("Category", "Unit")]
    public async Task PopulateOpenPullRequestCountWhenRepositoryHasSlugUpdatesCount()
    {
        // Arrange
        var expectedUrl = new Uri(
            "repositories/workspace/repo%20slug/pullrequests?state=OPEN&pagelen=1&fields=size",
            UriKind.Relative);
        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport.Setup(instance => instance.GetAsync<PullRequestPageSummaryDto>(
                expectedUrl,
                CancellationToken.None))
            .ReturnsAsync(new PullRequestPageSummaryDto(7));
        var client = CreateClient(transport: transport);
        var repository = CreateRepository();

        // Act
        await client.PopulateOpenPullRequestCountAsync(repository, CancellationToken.None);

        // Assert
        repository.OpenPullRequestsCount.Should().Be(7);
    }

    [Fact(DisplayName = "Populate count suppresses HTTP request failures")]
    [Trait("Category", "Unit")]
    public async Task PopulateOpenPullRequestCountWhenTransportFailsLeavesExistingCount()
    {
        // Arrange
        var transport = new Mock<IBitbucketTransport>();
        transport.Setup(instance => instance.GetAsync<PullRequestPageSummaryDto>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Failure"));
        var client = CreateClient(transport: transport);
        var repository = CreateRepository();
        repository.UpdateOpenPullRequestsCount(4);

        // Act
        await client.PopulateOpenPullRequestCountAsync(repository, CancellationToken.None);

        // Assert
        repository.OpenPullRequestsCount.Should().Be(4);
    }

    [Fact(DisplayName = "Get open details deletes cache when API returns no pull requests")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsWhenNoPullRequestsExistDeletesCache()
    {
        // Arrange
        var repository = CreateRepository();
        var currentUserId = new BitbucketId("current-user");
        var workspace = new BitbucketWorkspace("workspace");
        var transport = new Mock<IBitbucketTransport>();
        transport.Setup(instance => instance.GetAsync<PullRequestPageDto>(
                It.IsAny<Uri>(),
                CancellationToken.None))
            .ReturnsAsync(new PullRequestPageDto([], null));
        var cache = new Mock<IPullRequestDetailsCacheService>(MockBehavior.Strict);
        cache.Setup(instance => instance.ReadEntriesByPullRequestIdAsync(
                workspace,
                repository.Slug!.Value,
                currentUserId,
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<PullRequestId, PullRequestDetailsCacheEntry>());
        cache.Setup(instance => instance.DeleteAsync(
                workspace,
                repository.Slug!.Value,
                currentUserId,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        var client = CreateClient(transport: transport, cache: cache);

        // Act
        var result = await client.GetOpenPullRequestDetailsAsync(
            repository,
            currentUserId,
            CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        repository.OpenPullRequestsCount.Should().Be(0);
        cache.VerifyAll();
    }

    [Fact(DisplayName = "Get open details reuses cache and loads only changed activity")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsWhenCachePartiallyMatchesUsesExpectedSources()
    {
        // Arrange
        var repository = CreateRepository();
        var repositorySlug = repository.Slug!.Value;
        var currentUserId = new BitbucketId("current-user");
        var workspace = new BitbucketWorkspace("workspace");
        var firstDto = CreatePullRequestDto(1, "Cached");
        var secondDto = CreatePullRequestDto(2, "Fresh");
        var firstSnapshot = CreateSnapshot(1, "Cached", "fingerprint-1");
        var secondSnapshot = CreateSnapshot(2, "Fresh", "fingerprint-2");
        var cachedSummary = new PullRequestActivitySummary(
            firstSnapshot.CreatedOn.AddHours(1),
            firstSnapshot.CreatedOn.AddHours(2),
            true,
            3);
        var freshSummary = new PullRequestActivitySummary(
            secondSnapshot.CreatedOn.AddHours(1),
            secondSnapshot.CreatedOn.AddHours(3),
            false,
            2);
        var cachedEntry = new PullRequestDetailsCacheEntry(
            firstSnapshot.Id,
            "fingerprint-1",
            cachedSummary.FirstNonAuthorActivityOn,
            cachedSummary.LastActivityOn,
            cachedSummary.HasCurrentUserDiscussion,
            cachedSummary.CommentsCount);
        var freshEntry = new PullRequestDetailsCacheEntry(
            secondSnapshot.Id,
            "fingerprint-2",
            freshSummary.FirstNonAuthorActivityOn,
            freshSummary.LastActivityOn,
            freshSummary.HasCurrentUserDiscussion,
            freshSummary.CommentsCount);
        IReadOnlyDictionary<PullRequestId, PullRequestDetailsCacheEntry> cachedEntries =
            new Dictionary<PullRequestId, PullRequestDetailsCacheEntry>
            {
                [firstSnapshot.Id] = cachedEntry
            };
        var transport = new Mock<IBitbucketTransport>();
        transport.Setup(instance => instance.GetAsync<PullRequestPageDto>(
                It.IsAny<Uri>(),
                CancellationToken.None))
            .ReturnsAsync(new PullRequestPageDto([firstDto, secondDto], null));
        var mapper = new Mock<IPullRequestSnapshotMapper>(MockBehavior.Strict);
        mapper.Setup(instance => instance.CreateSnapshot(firstDto, currentUserId)).Returns(firstSnapshot);
        mapper.Setup(instance => instance.CreateSnapshot(secondDto, currentUserId)).Returns(secondSnapshot);
        var cache = new Mock<IPullRequestDetailsCacheService>(MockBehavior.Strict);
        cache.Setup(instance => instance.ReadEntriesByPullRequestIdAsync(
                workspace,
                repositorySlug,
                currentUserId,
                CancellationToken.None))
            .ReturnsAsync(cachedEntries);
        var cachedSummaryOut = cachedSummary;
        var cachedEntryOut = cachedEntry;
        cache.Setup(instance => instance.TryCreateActivitySummary(
                firstSnapshot,
                cachedEntries,
                out cachedSummaryOut,
                out cachedEntryOut))
            .Returns(true);
        PullRequestActivitySummary unusedSummary = null!;
        PullRequestDetailsCacheEntry unusedEntry = null!;
        cache.Setup(instance => instance.TryCreateActivitySummary(
                secondSnapshot,
                cachedEntries,
                out unusedSummary,
                out unusedEntry))
            .Returns(false);
        cache.Setup(instance => instance.CreateEntry(secondSnapshot, freshSummary)).Returns(freshEntry);
        cache.Setup(instance => instance.SaveEntriesAsync(
                workspace,
                repositorySlug,
                currentUserId,
                It.Is<IReadOnlyCollection<PullRequestDetailsCacheEntry>>(entries =>
                    entries.Count == 2
                    && entries.Contains(cachedEntry)
                    && entries.Contains(freshEntry)),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        PullRequestActivityEntry[] freshActivities =
        [
            new(new BitbucketId("reviewer"), secondSnapshot.CreatedOn.AddHours(1), true)
        ];
        var activityLoader = new Mock<IBitbucketPullRequestActivityLoader>(MockBehavior.Strict);
        activityLoader.Setup(instance => instance.GetActivitiesAsync(
                repositorySlug,
                secondSnapshot.Id,
                CancellationToken.None))
            .ReturnsAsync(freshActivities);
        var analyzer = new Mock<IPullRequestActivityAnalyzer>(MockBehavior.Strict);
        analyzer.Setup(instance => instance.CreateSummary(freshActivities, secondSnapshot, currentUserId))
            .Returns(freshSummary);
        var client = CreateClient(
            transport,
            analyzer,
            activityLoader,
            mapper,
            cache);

        // Act
        var result = await client.GetOpenPullRequestDetailsAsync(
            repository,
            currentUserId,
            CancellationToken.None);

        // Assert
        result.Select(detail => detail.Title).Should().Equal("Cached", "Fresh");
        result.Select(detail => detail.CommentsCount).Should().Equal(3, 2);
        repository.OpenPullRequestsCount.Should().Be(2);
        activityLoader.Verify(instance => instance.GetActivitiesAsync(
            repositorySlug,
            firstSnapshot.Id,
            It.IsAny<CancellationToken>()), Times.Never);
        cache.VerifyAll();
    }

    [Fact(DisplayName = "Get merged pull requests stops after first item older than boundary")]
    [Trait("Category", "Unit")]
    public async Task GetMergedPullRequestsWhenSortedPageCrossesBoundaryStopsLoading()
    {
        // Arrange
        var repository = CreateRepository();
        var repositorySlug = repository.Slug!.Value;
        var currentUserId = new BitbucketId("current-user");
        var mergedSince = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var recentDto = CreatePullRequestDto(10, "Recent", mergedSince.AddDays(2));
        var oldDto = CreatePullRequestDto(9, "Old", mergedSince.AddMinutes(-1));
        var ignoredDto = CreatePullRequestDto(8, "Ignored", mergedSince.AddDays(1));
        var recentSnapshot = CreateSnapshot(10, "Recent", "fingerprint");
        var summary = new PullRequestActivitySummary(
            recentSnapshot.CreatedOn.AddHours(1),
            recentSnapshot.CreatedOn.AddHours(2),
            true,
            4);
        var nextUrl = new Uri("next-page", UriKind.Relative);
        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport.Setup(instance => instance.GetAsync<PullRequestPageDto>(
                It.Is<Uri>(url => url.OriginalString.Contains("state=MERGED", StringComparison.Ordinal)),
                CancellationToken.None))
            .ReturnsAsync(new PullRequestPageDto([recentDto, oldDto, ignoredDto], nextUrl));
        var mapper = new Mock<IPullRequestSnapshotMapper>(MockBehavior.Strict);
        mapper.Setup(instance => instance.CreateSnapshot(recentDto, currentUserId)).Returns(recentSnapshot);
        var activities = Array.Empty<PullRequestActivityEntry>();
        var activityLoader = new Mock<IBitbucketPullRequestActivityLoader>(MockBehavior.Strict);
        activityLoader.Setup(instance => instance.GetActivitiesAsync(
                repositorySlug,
                recentSnapshot.Id,
                CancellationToken.None))
            .ReturnsAsync(activities);
        var analyzer = new Mock<IPullRequestActivityAnalyzer>(MockBehavior.Strict);
        analyzer.Setup(instance => instance.CreateSummary(activities, recentSnapshot, currentUserId))
            .Returns(summary);
        var client = CreateClient(
            transport,
            analyzer,
            activityLoader,
            mapper);

        // Act
        var result = await client.GetMergedPullRequestsAsync(
            repository,
            mergedSince,
            currentUserId,
            CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        result[0].Title.Should().Be("Recent");
        result[0].MergedOn.Should().Be(recentDto.UpdatedOn);
        result[0].CommentsCount.Should().Be(4);
        transport.Verify(instance => instance.GetAsync<PullRequestPageDto>(
            nextUrl,
            It.IsAny<CancellationToken>()), Times.Never);
        mapper.Verify(instance => instance.CreateSnapshot(
            It.Is<PullRequestDto>(dto => dto.Id != recentDto.Id),
            It.IsAny<BitbucketId>()), Times.Never);
    }

    [Fact(DisplayName = "Get open details suppresses HTTP request failures")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsWhenTransportFailsReturnsEmptyList()
    {
        // Arrange
        var repository = CreateRepository();
        var currentUserId = new BitbucketId("current-user");
        var cache = new Mock<IPullRequestDetailsCacheService>();
        cache.Setup(instance => instance.ReadEntriesByPullRequestIdAsync(
                It.IsAny<BitbucketWorkspace>(),
                It.IsAny<RepositorySlug>(),
                It.IsAny<BitbucketId>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<PullRequestId, PullRequestDetailsCacheEntry>());
        var transport = new Mock<IBitbucketTransport>();
        transport.Setup(instance => instance.GetAsync<PullRequestPageDto>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Failure"));
        var client = CreateClient(transport: transport, cache: cache);

        // Act
        var result = await client.GetOpenPullRequestDetailsAsync(
            repository,
            currentUserId,
            CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    private static BitbucketPRApiClient CreateClient(
        Mock<IBitbucketTransport>? transport = null,
        Mock<IPullRequestActivityAnalyzer>? analyzer = null,
        Mock<IBitbucketPullRequestActivityLoader>? activityLoader = null,
        Mock<IPullRequestSnapshotMapper>? mapper = null,
        Mock<IPullRequestDetailsCacheService>? cache = null) =>
        new(
            (transport ?? new Mock<IBitbucketTransport>()).Object,
            (analyzer ?? new Mock<IPullRequestActivityAnalyzer>()).Object,
            (activityLoader ?? new Mock<IBitbucketPullRequestActivityLoader>()).Object,
            (mapper ?? new Mock<IPullRequestSnapshotMapper>()).Object,
            (cache ?? new Mock<IPullRequestDetailsCacheService>()).Object,
            CreateOptions());

    private static Repository CreateRepository() =>
        new("Repository", slug: new RepositorySlug("repo slug"));

    private static PullRequestDto CreatePullRequestDto(
        int id,
        string title,
        DateTimeOffset? updatedOn = null) =>
        new(
            Id: id,
            Title: title,
            CreatedOn: new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
            UpdatedOn: updatedOn ?? new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));

    private static PullRequestSnapshot CreateSnapshot(int id, string title, string fingerprint) =>
        new(
            new PullRequestId(id),
            title,
            new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
            DescriptionText: "Description",
            AuthorId: new BitbucketId("author"),
            AuthorDisplayName: "Author",
            RequestChangesCount: 1,
            HasCurrentUserRequestChanges: true,
            ApprovalsCount: 2,
            HasCurrentUserApproval: false,
            CacheFingerprint: fingerprint);

    private static IOptions<BitbucketOptions> CreateOptions() =>
        Options.Create(new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.example.test/"),
            Workspace = "workspace",
            PageLen = 25
        });
}
