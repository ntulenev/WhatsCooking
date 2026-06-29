using BBRepoList.Models;

namespace BBRepoList.API;

/// <summary>
/// Builds Bitbucket pull request endpoint URLs.
/// </summary>
public interface IBitbucketPullRequestUrlBuilder
{
    /// <summary>
    /// Creates URL for loading open pull request count.
    /// </summary>
    /// <param name="repositorySlug">Repository slug.</param>
    /// <returns>Relative Bitbucket API URL.</returns>
    Uri CreateOpenPullRequestCountUrl(RepositorySlug repositorySlug);

    /// <summary>
    /// Creates URL for loading recently merged pull request pages.
    /// </summary>
    /// <param name="repositorySlug">Repository slug.</param>
    /// <returns>Relative Bitbucket API URL.</returns>
    Uri CreateMergedPullRequestsUrl(RepositorySlug repositorySlug);

    /// <summary>
    /// Creates URL for loading open pull request snapshot pages.
    /// </summary>
    /// <param name="repositorySlug">Repository slug.</param>
    /// <returns>Relative Bitbucket API URL.</returns>
    Uri CreateOpenPullRequestSnapshotsUrl(RepositorySlug repositorySlug);
}
