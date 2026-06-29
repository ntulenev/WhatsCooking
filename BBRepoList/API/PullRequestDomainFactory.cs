using BBRepoList.Models;

namespace BBRepoList.API;

/// <summary>
/// Default factory for pull request domain models.
/// </summary>
public sealed class PullRequestDomainFactory : IPullRequestDomainFactory
{
    /// <inheritdoc />
    public PullRequestDetail CreateDetail(
        Repository repository,
        PullRequestSnapshot pullRequest,
        PullRequestActivitySummary activitySummary)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(activitySummary);

        return new PullRequestDetail(
            repository,
            pullRequest.Id,
            pullRequest.Title,
            pullRequest.CreatedOn,
            pullRequest.AuthorId,
            pullRequest.AuthorDisplayName,
            activitySummary.FirstNonAuthorActivityOn,
            activitySummary.LastActivityOn,
            activitySummary.HasCurrentUserDiscussion,
            pullRequest.DescriptionText,
            activitySummary.CommentsCount,
            pullRequest.RequestChangesCount,
            pullRequest.HasCurrentUserRequestChanges,
            pullRequest.ApprovalsCount,
            pullRequest.HasCurrentUserApproval);
    }

    /// <inheritdoc />
    public MergedPullRequest CreateMerged(
        Repository repository,
        PullRequestSnapshot pullRequest,
        DateTimeOffset mergedOn,
        PullRequestActivitySummary activitySummary)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(activitySummary);

        return new MergedPullRequest(
            repository,
            pullRequest.Id,
            pullRequest.Title,
            pullRequest.CreatedOn,
            pullRequest.AuthorId,
            pullRequest.AuthorDisplayName,
            activitySummary.FirstNonAuthorActivityOn,
            activitySummary.LastActivityOn,
            activitySummary.HasCurrentUserDiscussion,
            mergedOn,
            pullRequest.DescriptionText,
            activitySummary.CommentsCount,
            pullRequest.RequestChangesCount,
            pullRequest.HasCurrentUserRequestChanges,
            pullRequest.ApprovalsCount,
            pullRequest.HasCurrentUserApproval);
    }
}
