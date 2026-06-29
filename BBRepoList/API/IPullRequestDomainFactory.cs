using BBRepoList.Models;

namespace BBRepoList.API;

/// <summary>
/// Creates pull request domain models from loaded snapshots and activity summaries.
/// </summary>
public interface IPullRequestDomainFactory
{
    /// <summary>
    /// Creates an open pull request detail.
    /// </summary>
    /// <param name="repository">Source repository.</param>
    /// <param name="pullRequest">Pull request snapshot.</param>
    /// <param name="activitySummary">Pull request activity summary.</param>
    /// <returns>Open pull request detail.</returns>
    PullRequestDetail CreateDetail(
        Repository repository,
        PullRequestSnapshot pullRequest,
        PullRequestActivitySummary activitySummary);

    /// <summary>
    /// Creates a merged pull request.
    /// </summary>
    /// <param name="repository">Source repository.</param>
    /// <param name="pullRequest">Pull request snapshot.</param>
    /// <param name="mergedOn">Pull request merge timestamp.</param>
    /// <param name="activitySummary">Pull request activity summary.</param>
    /// <returns>Merged pull request.</returns>
    MergedPullRequest CreateMerged(
        Repository repository,
        PullRequestSnapshot pullRequest,
        DateTimeOffset mergedOn,
        PullRequestActivitySummary activitySummary);
}
