using BBRepoList.Models;

using FluentAssertions;

using System.Text.Json.Nodes;

namespace BBRepoList.Caching.Tests;

public sealed class FilePullRequestDetailsCacheTests
{
    [Fact(DisplayName = "Read entries returns empty collection when cache does not exist")]
    [Trait("Category", "Unit")]
    public async Task ReadEntriesAsyncWhenCacheDoesNotExistReturnsEmptyCollection()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        var cache = new FilePullRequestDetailsCache(directory.Path);

        // Act
        var result = await cache.ReadEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            cancellation.Token);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "Save entries persists valid entries ordered by pull request id")]
    [Trait("Category", "Unit")]
    public async Task SaveEntriesAsyncWhenEntriesAreValidPersistsOrderedEntries()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        var cache = new FilePullRequestDetailsCache(directory.Path);
        PullRequestDetailsCacheEntry[] entries =
        [
            CreateEntry(2, "second"),
            CreateEntry(1, "first")
        ];

        // Act
        await cache.SaveEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            entries,
            cancellation.Token);
        var result = await cache.ReadEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            cancellation.Token);

        // Assert
        result.Should().Equal(entries.Reverse());
    }

    [Fact(DisplayName = "Open and merged entries are stored independently")]
    [Trait("Category", "Unit")]
    public async Task SaveEntriesAsyncWhenScopesDifferKeepsIndependentEntries()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        var cache = new FilePullRequestDetailsCache(directory.Path);
        var openEntry = CreateEntry(1, "open");
        var mergedEntry = CreateEntry(2, "merged");

        // Act
        await cache.SaveEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            PullRequestDetailsCacheScope.Open,
            [openEntry],
            cancellation.Token);
        await cache.SaveEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            PullRequestDetailsCacheScope.Merged,
            [mergedEntry],
            cancellation.Token);
        var openResult = await cache.ReadEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            PullRequestDetailsCacheScope.Open,
            cancellation.Token);
        var mergedResult = await cache.ReadEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            PullRequestDetailsCacheScope.Merged,
            cancellation.Token);

        // Assert
        openResult.Should().Equal(openEntry);
        mergedResult.Should().Equal(mergedEntry);
    }

    [Fact(DisplayName = "Save entries excludes entries with invalid cached values")]
    [Trait("Category", "Unit")]
    public async Task SaveEntriesAsyncWhenEntriesContainInvalidValuesPersistsOnlyValidEntries()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        var cache = new FilePullRequestDetailsCache(directory.Path);
        var validEntry = CreateEntry(1, "valid");
        PullRequestDetailsCacheEntry[] entries =
        [
            validEntry,
            CreateEntry(2, " "),
            CreateEntry(3, "invalid-comments", commentsCount: -1)
        ];

        // Act
        await cache.SaveEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            entries,
            cancellation.Token);
        var result = await cache.ReadEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            cancellation.Token);

        // Assert
        result.Should().ContainSingle().Which.Should().Be(validEntry);
    }

    [Fact(DisplayName = "Save entries deletes cache when all entries are invalid")]
    [Trait("Category", "Unit")]
    public async Task SaveEntriesAsyncWhenAllEntriesAreInvalidDeletesExistingCache()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        var cache = new FilePullRequestDetailsCache(directory.Path);
        await cache.SaveEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            [CreateEntry(1, "existing")],
            cancellation.Token);
        PullRequestDetailsCacheEntry[] invalidEntries =
        [
            CreateEntry(2, " "),
            CreateEntry(3, "invalid-comments", commentsCount: -1)
        ];

        // Act
        await cache.SaveEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            invalidEntries,
            cancellation.Token);

        // Assert
        Directory.EnumerateFiles(directory.Path, "*.json", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact(DisplayName = "Save empty entries deletes existing cache")]
    [Trait("Category", "Unit")]
    public async Task SaveEntriesAsyncWhenEntriesAreEmptyDeletesExistingCache()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        var cache = new FilePullRequestDetailsCache(directory.Path);
        await cache.SaveEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            [CreateEntry(1, "fingerprint")],
            cancellation.Token);

        // Act
        await cache.SaveEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            [],
            cancellation.Token);
        var result = await cache.ReadEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            cancellation.Token);

        // Assert
        result.Should().BeEmpty();
        Directory.EnumerateFiles(directory.Path, "*.json", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact(DisplayName = "Read entries returns empty collection when cache JSON is malformed")]
    [Trait("Category", "Unit")]
    public async Task ReadEntriesAsyncWhenCacheJsonIsMalformedReturnsEmptyCollection()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        var cache = new FilePullRequestDetailsCache(directory.Path);
        await cache.SaveEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            [CreateEntry(1, "fingerprint")],
            cancellation.Token);
        var cacheFilePath = Directory.EnumerateFiles(directory.Path, "*.json", SearchOption.AllDirectories).Single();
        await File.WriteAllTextAsync(cacheFilePath, "{", cancellation.Token);

        // Act
        var result = await cache.ReadEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            cancellation.Token);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "Read entries returns empty collection when cache file is locked")]
    [Trait("Category", "Integration")]
    public async Task ReadEntriesAsyncWhenCacheFileIsLockedReturnsEmptyCollection()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var cache = new FilePullRequestDetailsCache(directory.Path);
        await cache.SaveEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            [CreateEntry(1, "fingerprint")],
            CancellationToken.None);
        var cacheFilePath = Directory.EnumerateFiles(directory.Path, "*.json", SearchOption.AllDirectories).Single();
        using var lockedFile = new FileStream(cacheFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        // Act
        var result = await cache.ReadEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "Read entries ignores cache documents with unsupported versions")]
    [Trait("Category", "Unit")]
    public async Task ReadEntriesAsyncWhenCacheVersionIsUnsupportedReturnsEmptyCollection()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        var cache = new FilePullRequestDetailsCache(directory.Path);
        await cache.SaveEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            [CreateEntry(1, "fingerprint")],
            cancellation.Token);
        var cacheFilePath = Directory.EnumerateFiles(directory.Path, "*.json", SearchOption.AllDirectories).Single();
        var document = JsonNode.Parse(await File.ReadAllTextAsync(cacheFilePath, cancellation.Token))!.AsObject();
        document["Version"] = 2;
        await File.WriteAllTextAsync(cacheFilePath, document.ToJsonString(), cancellation.Token);

        // Act
        var result = await cache.ReadEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            cancellation.Token);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "Save entries throws when entries are null")]
    [Trait("Category", "Unit")]
    public async Task SaveEntriesAsyncWhenEntriesAreNullThrowsArgumentNullException()
    {
        // Arrange
        var cache = new FilePullRequestDetailsCache();
        IReadOnlyCollection<PullRequestDetailsCacheEntry> entries = null!;

        // Act
        Func<Task> act = () => cache.SaveEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            entries,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "Delete throws when cancellation is requested")]
    [Trait("Category", "Unit")]
    public async Task DeleteAsyncWhenCancellationIsRequestedThrowsOperationCanceledException()
    {
        // Arrange
        var cache = new FilePullRequestDetailsCache();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        Func<Task> act = () => cache.DeleteAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(DisplayName = "Save entries ignores failures to create the cache directory")]
    [Trait("Category", "Integration")]
    public async Task SaveEntriesAsyncWhenCacheRootIsAFileDoesNotThrow()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var blockingFilePath = Path.Combine(directory.Path, "blocking-file");
        await File.WriteAllTextAsync(
            blockingFilePath,
            "content",
            TestContext.Current.CancellationToken);
        var cache = new FilePullRequestDetailsCache(blockingFilePath);

        // Act
        Func<Task> act = () => cache.SaveEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            [CreateEntry(1, "fingerprint")],
            CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        Directory.EnumerateFiles(directory.Path, "*.tmp", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact(DisplayName = "Delete ignores failures when cache file is locked")]
    [Trait("Category", "Integration")]
    public async Task DeleteAsyncWhenCacheFileIsLockedDoesNotThrow()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var cache = new FilePullRequestDetailsCache(directory.Path);
        await cache.SaveEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            [CreateEntry(1, "fingerprint")],
            CancellationToken.None);
        var cacheFilePath = Directory.EnumerateFiles(directory.Path, "*.json", SearchOption.AllDirectories).Single();
        using var lockedFile = new FileStream(cacheFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        // Act
        Func<Task> act = () => cache.DeleteAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            _scope,
            CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        File.Exists(cacheFilePath).Should().BeTrue();
    }

    [Fact(DisplayName = "Cache rejects unsupported pull request scope")]
    [Trait("Category", "Unit")]
    public async Task ReadEntriesAsyncWhenScopeIsUnsupportedThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var cache = new FilePullRequestDetailsCache(directory.Path);
        var unsupportedScope = (PullRequestDetailsCacheScope)int.MaxValue;

        // Act
        Func<Task> act = () => cache.ReadEntriesAsync(
            _workspace,
            _repositorySlug,
            _currentUserId,
            unsupportedScope,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("scope");
    }

    private static PullRequestDetailsCacheEntry CreateEntry(
        int pullRequestId,
        string fingerprint,
        int commentsCount = 2) =>
        new(
            new PullRequestId(pullRequestId),
            fingerprint,
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 2, 11, 0, 0, TimeSpan.Zero),
            true,
            commentsCount);

    private static readonly BitbucketWorkspace _workspace = new("workspace");
    private static readonly RepositorySlug _repositorySlug = new("repository");
    private static readonly BitbucketId _currentUserId = new("user");
    private const PullRequestDetailsCacheScope _scope = PullRequestDetailsCacheScope.Merged;

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "WhatsCooking.Tests",
                Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
