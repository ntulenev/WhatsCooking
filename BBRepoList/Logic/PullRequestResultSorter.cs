using BBRepoList.Models;

namespace BBRepoList.Logic;

/// <summary>
/// Sorts pull request results for repository reports.
/// </summary>
public sealed class PullRequestResultSorter : IPullRequestResultSorter
{
    /// <inheritdoc />
    public IReadOnlyList<PullRequestDetail> SortOpen(IReadOnlyList<PullRequestDetail> pullRequests)
    {
        ArgumentNullException.ThrowIfNull(pullRequests);

        return
        [
            .. pullRequests
                .OrderByDescending(static detail => detail.OpenedOn)
                .ThenBy(static detail => detail.RepositoryName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static detail => detail.PullRequestId.Value)
        ];
    }

    /// <inheritdoc />
    public IReadOnlyList<MergedPullRequest> SortMerged(IReadOnlyList<MergedPullRequest> pullRequests)
    {
        ArgumentNullException.ThrowIfNull(pullRequests);

        return
        [
            .. pullRequests
                .OrderByDescending(static pullRequest => pullRequest.MergedOn)
                .ThenBy(static pullRequest => pullRequest.RepositoryName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static pullRequest => pullRequest.PullRequestId.Value)
        ];
    }
}
