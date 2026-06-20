using BBRepoList.Abstractions;
using BBRepoList.Models;

namespace BBRepoList.Logic;

/// <summary>
/// Compares pull request collections by repository identity and pull request number.
/// </summary>
public sealed class PullRequestDiffService : IPullRequestDiffService
{
    /// <inheritdoc />
    public PullRequestDiffSummary Compare(
        IReadOnlyCollection<PullRequestDetail> previousOpenPullRequests,
        IReadOnlyCollection<MergedPullRequest> previousMergedPullRequests,
        IReadOnlyCollection<PullRequestDetail> currentOpenPullRequests,
        IReadOnlyCollection<MergedPullRequest> currentMergedPullRequests)
    {
        ArgumentNullException.ThrowIfNull(previousOpenPullRequests);
        ArgumentNullException.ThrowIfNull(previousMergedPullRequests);
        ArgumentNullException.ThrowIfNull(currentOpenPullRequests);
        ArgumentNullException.ThrowIfNull(currentMergedPullRequests);

        var previousOpenPullRequestKeys = CreateOpenPullRequestKeys(previousOpenPullRequests);
        var previousMergedPullRequestKeys = CreateMergedPullRequestKeys(previousMergedPullRequests);
        var newOpenPullRequestsCount = currentOpenPullRequests.Count(
            pullRequest => !previousOpenPullRequestKeys.Contains(CreatePullRequestKey(pullRequest)));
        var newMergedPullRequestsCount = currentMergedPullRequests.Count(
            pullRequest => !previousMergedPullRequestKeys.Contains(CreatePullRequestKey(pullRequest)));

        return new PullRequestDiffSummary(newOpenPullRequestsCount, newMergedPullRequestsCount);
    }

    private static HashSet<PullRequestKey> CreateOpenPullRequestKeys(IEnumerable<PullRequestDetail> pullRequests) =>
        [.. pullRequests.Select(CreatePullRequestKey)];

    private static HashSet<PullRequestKey> CreateMergedPullRequestKeys(IEnumerable<MergedPullRequest> pullRequests) =>
        [.. pullRequests.Select(CreatePullRequestKey)];

    private static PullRequestKey CreatePullRequestKey(PullRequestDetail pullRequest) =>
        new(GetRepositoryKey(pullRequest.RepositorySlug, pullRequest.RepositoryName), pullRequest.PullRequestId.Value);

    private static PullRequestKey CreatePullRequestKey(MergedPullRequest pullRequest) =>
        new(GetRepositoryKey(pullRequest.RepositorySlug, pullRequest.RepositoryName), pullRequest.PullRequestId.Value);

    private static string GetRepositoryKey(RepositorySlug? slug, string repositoryName) =>
        slug?.Value ?? repositoryName;

    private readonly record struct PullRequestKey(string RepositoryKey, int PullRequestId);
}
