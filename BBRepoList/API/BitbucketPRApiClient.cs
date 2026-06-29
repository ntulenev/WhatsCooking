using BBRepoList.Abstractions;
using BBRepoList.Models;

namespace BBRepoList.API;

/// <summary>
/// Bitbucket REST API client facade for pull request operations.
/// </summary>
public sealed class BitbucketPRApiClient : IBitbucketPRApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketPRApiClient"/> class.
    /// </summary>
    /// <param name="openPullRequests">Open pull request workflow service.</param>
    /// <param name="mergedPullRequests">Merged pull request workflow service.</param>
    public BitbucketPRApiClient(
        IBitbucketOpenPullRequestService openPullRequests,
        IBitbucketMergedPullRequestService mergedPullRequests)
    {
        ArgumentNullException.ThrowIfNull(openPullRequests);
        ArgumentNullException.ThrowIfNull(mergedPullRequests);

        _openPullRequests = openPullRequests;
        _mergedPullRequests = mergedPullRequests;
    }

    /// <inheritdoc />
    public Task PopulateOpenPullRequestCountAsync(Repository repository, CancellationToken cancellationToken) =>
        _openPullRequests.PopulateOpenPullRequestCountAsync(repository, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<PullRequestDetail>> GetOpenPullRequestDetailsAsync(
        Repository repository,
        BitbucketId currentUserId,
        CancellationToken cancellationToken) =>
        _openPullRequests.GetOpenPullRequestDetailsAsync(repository, currentUserId, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<MergedPullRequest>> GetMergedPullRequestsAsync(
        Repository repository,
        DateTimeOffset mergedSince,
        BitbucketId currentUserId,
        CancellationToken cancellationToken) =>
        _mergedPullRequests.GetMergedPullRequestsAsync(repository, mergedSince, currentUserId, cancellationToken);

    private readonly IBitbucketOpenPullRequestService _openPullRequests;
    private readonly IBitbucketMergedPullRequestService _mergedPullRequests;
}
