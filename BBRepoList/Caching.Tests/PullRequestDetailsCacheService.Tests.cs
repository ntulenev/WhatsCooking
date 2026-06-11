using BBRepoList.Abstractions;
using BBRepoList.Models;

using FluentAssertions;

using Moq;

namespace BBRepoList.Caching.Tests;

public sealed class PullRequestDetailsCacheServiceTests
{
    [Fact(DisplayName = "Constructor throws when cache is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCacheIsNullThrowsArgumentNullException()
    {
        // Arrange
        IPullRequestDetailsCache cache = null!;

        // Act
        Action act = () => _ = new PullRequestDetailsCacheService(cache);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Read entries keeps last duplicate pull request entry")]
    [Trait("Category", "Unit")]
    public async Task ReadEntriesByPullRequestIdAsyncWhenEntriesContainDuplicatesKeepsLastEntry()
    {
        // Arrange
        var firstEntry = CreateEntry(1, "first");
        var lastEntry = CreateEntry(1, "last");
        using var cancellation = new CancellationTokenSource();
        var cache = new Mock<IPullRequestDetailsCache>();
        _ = cache
            .Setup(instance => instance.ReadEntriesAsync(
                _workspace,
                _repositorySlug,
                _currentUserId,
                cancellation.Token))
            .ReturnsAsync([firstEntry, lastEntry]);
        var service = new PullRequestDetailsCacheService(cache.Object);

        // Act
        var result = await service.ReadEntriesByPullRequestIdAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            cancellation.Token);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<PullRequestId, PullRequestDetailsCacheEntry>(
                firstEntry.PullRequestId,
                lastEntry));
    }

    [Fact(DisplayName = "Save entries delegates to cache")]
    [Trait("Category", "Unit")]
    public async Task SaveEntriesAsyncDelegatesToCache()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        PullRequestDetailsCacheEntry[] entries = [CreateEntry(1, "fingerprint")];
        var cache = new Mock<IPullRequestDetailsCache>(MockBehavior.Strict);
        cache.Setup(instance => instance.SaveEntriesAsync(
                _workspace,
                _repositorySlug,
                _currentUserId,
                entries,
                cancellation.Token))
            .Returns(Task.CompletedTask);
        var service = new PullRequestDetailsCacheService(cache.Object);

        // Act
        await service.SaveEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            entries,
            cancellation.Token);

        // Assert
        cache.VerifyAll();
    }

    [Fact(DisplayName = "Delete delegates to cache")]
    [Trait("Category", "Unit")]
    public async Task DeleteAsyncDelegatesToCache()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var cache = new Mock<IPullRequestDetailsCache>(MockBehavior.Strict);
        cache.Setup(instance => instance.DeleteAsync(
                _workspace,
                _repositorySlug,
                _currentUserId,
                cancellation.Token))
            .Returns(Task.CompletedTask);
        var service = new PullRequestDetailsCacheService(cache.Object);

        // Act
        await service.DeleteAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            cancellation.Token);

        // Assert
        cache.VerifyAll();
    }

    [Fact(DisplayName = "Try create activity summary returns cached values when fingerprint matches")]
    [Trait("Category", "Unit")]
    public void TryCreateActivitySummaryWhenFingerprintMatchesReturnsCachedValues()
    {
        // Arrange
        var service = new PullRequestDetailsCacheService(Mock.Of<IPullRequestDetailsCache>());
        var pullRequest = CreateSnapshot("fingerprint");
        var entry = CreateEntry(pullRequest.Id.Value, "fingerprint");
        IReadOnlyDictionary<PullRequestId, PullRequestDetailsCacheEntry> entries =
            new Dictionary<PullRequestId, PullRequestDetailsCacheEntry>
            {
                [pullRequest.Id] = entry
            };

        // Act
        var result = service.TryCreateActivitySummary(
            pullRequest,
            entries,
            out var activitySummary,
            out var cacheEntry);

        // Assert
        result.Should().BeTrue();
        cacheEntry.Should().Be(entry);
        activitySummary.Should().Be(new PullRequestActivitySummary(
            entry.FirstNonAuthorActivityOn,
            entry.LastActivityOn,
            entry.HasCurrentUserDiscussion,
            entry.CommentsCount));
    }

    [Theory(DisplayName = "Try create activity summary rejects missing or different fingerprints")]
    [Trait("Category", "Unit")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("different")]
    public void TryCreateActivitySummaryWhenFingerprintDoesNotMatchReturnsFalse(string? fingerprint)
    {
        // Arrange
        var service = new PullRequestDetailsCacheService(Mock.Of<IPullRequestDetailsCache>());
        var pullRequest = CreateSnapshot(fingerprint);
        var entry = CreateEntry(pullRequest.Id.Value, "fingerprint");
        IReadOnlyDictionary<PullRequestId, PullRequestDetailsCacheEntry> entries =
            new Dictionary<PullRequestId, PullRequestDetailsCacheEntry>
            {
                [pullRequest.Id] = entry
            };

        // Act
        var result = service.TryCreateActivitySummary(
            pullRequest,
            entries,
            out var activitySummary,
            out var cacheEntry);

        // Assert
        result.Should().BeFalse();
        activitySummary.Should().BeNull();
        cacheEntry.Should().BeNull();
    }

    [Fact(DisplayName = "Try create activity summary throws when entries are null")]
    [Trait("Category", "Unit")]
    public void TryCreateActivitySummaryWhenEntriesAreNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new PullRequestDetailsCacheService(Mock.Of<IPullRequestDetailsCache>());
        var pullRequest = CreateSnapshot("fingerprint");
        IReadOnlyDictionary<PullRequestId, PullRequestDetailsCacheEntry> entries = null!;

        // Act
        Action act = () => service.TryCreateActivitySummary(
            pullRequest,
            entries,
            out _,
            out _);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Create entry maps pull request and activity summary values")]
    [Trait("Category", "Unit")]
    public void CreateEntryWhenValuesAreValidReturnsMappedEntry()
    {
        // Arrange
        var service = new PullRequestDetailsCacheService(Mock.Of<IPullRequestDetailsCache>());
        var pullRequest = CreateSnapshot("fingerprint");
        var activitySummary = new PullRequestActivitySummary(
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 2, 11, 0, 0, TimeSpan.Zero),
            true,
            3);

        // Act
        var result = service.CreateEntry(pullRequest, activitySummary);

        // Assert
        result.Should().Be(new PullRequestDetailsCacheEntry(
            pullRequest.Id,
            "fingerprint",
            activitySummary.FirstNonAuthorActivityOn,
            activitySummary.LastActivityOn,
            activitySummary.HasCurrentUserDiscussion,
            activitySummary.CommentsCount));
    }

    [Fact(DisplayName = "Create entry throws when activity summary is null")]
    [Trait("Category", "Unit")]
    public void CreateEntryWhenActivitySummaryIsNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new PullRequestDetailsCacheService(Mock.Of<IPullRequestDetailsCache>());
        var pullRequest = CreateSnapshot("fingerprint");
        PullRequestActivitySummary activitySummary = null!;

        // Act
        Action act = () => service.CreateEntry(pullRequest, activitySummary);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private static PullRequestSnapshot CreateSnapshot(string? fingerprint) =>
        new(
            new PullRequestId(42),
            "Pull request",
            new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero),
            CacheFingerprint: fingerprint);

    private static PullRequestDetailsCacheEntry CreateEntry(int pullRequestId, string fingerprint) =>
        new(
            new PullRequestId(pullRequestId),
            fingerprint,
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 2, 11, 0, 0, TimeSpan.Zero),
            true,
            2);

    private static readonly BitbucketWorkspace _workspace = new("workspace");
    private static readonly RepositorySlug _repositorySlug = new("repository");
    private static readonly BitbucketId _currentUserId = new("user");
}
