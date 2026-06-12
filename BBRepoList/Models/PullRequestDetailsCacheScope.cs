namespace BBRepoList.Models;

/// <summary>
/// Identifies the pull request population stored in a details cache.
/// </summary>
public enum PullRequestDetailsCacheScope
{
    /// <summary>
    /// Open pull requests.
    /// </summary>
    Open,

    /// <summary>
    /// Merged pull requests.
    /// </summary>
    Merged
}
