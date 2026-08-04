namespace WhatsCooking.ViewModels;

/// <summary>
/// Executes user-triggered dashboard actions.
/// </summary>
internal interface IDashboardUserActions
{
    /// <summary>
    /// Opens a pull request URL.
    /// </summary>
    /// <param name="url">URL to open.</param>
    void OpenUrl(Uri url);

    /// <summary>
    /// Copies an AI review prompt for a pull request.
    /// </summary>
    /// <param name="pullRequest">Pull request row.</param>
    /// <returns>Status text for the completed action.</returns>
    string CopyAiReviewPrompt(PullRequestRow pullRequest);

    /// <summary>
    /// Copies an AI team overview prompt for pull requests.
    /// </summary>
    /// <param name="pullRequests">Pull request rows.</param>
    /// <param name="isMerged">Whether the pull requests are already merged.</param>
    /// <returns>Status text for the completed action.</returns>
    string CopyAiTeamOverviewPrompt(
        IReadOnlyCollection<PullRequestRow> pullRequests,
        bool isMerged);
}
