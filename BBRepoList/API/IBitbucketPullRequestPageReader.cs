using BBRepoList.Transport;

namespace BBRepoList.API;

/// <summary>
/// Reads Bitbucket pull request DTO pages.
/// </summary>
public interface IBitbucketPullRequestPageReader
{
    /// <summary>
    /// Iterates pull request DTOs starting from the provided URL.
    /// </summary>
    /// <param name="initialUrl">Initial relative Bitbucket API URL.</param>
    /// <param name="handlePullRequest">Callback for each pull request DTO. Return <see langword="false"/> to stop iteration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ForEachAsync(
        Uri initialUrl,
        Func<PullRequestDto, CancellationToken, ValueTask<bool>> handlePullRequest,
        CancellationToken cancellationToken);
}
