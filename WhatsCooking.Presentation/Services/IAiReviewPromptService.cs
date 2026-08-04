using WhatsCooking.ViewModels;

namespace WhatsCooking.Services;

/// <summary>
/// Creates and copies AI pull request prompts.
/// </summary>
internal interface IAiReviewPromptService
{
    /// <summary>
    /// Copies an AI review prompt for the pull request.
    /// </summary>
    /// <param name="pullRequest">Pull request row to review.</param>
    void CopyPrompt(PullRequestRow pullRequest);

    /// <summary>
    /// Copies a team overview prompt for the supplied pull requests.
    /// </summary>
    /// <param name="pullRequests">Pull request rows to analyze.</param>
    /// <param name="isMerged">Whether the pull requests are already merged.</param>
    void CopyTeamOverviewPrompt(
        IReadOnlyCollection<PullRequestRow> pullRequests,
        bool isMerged);
}
