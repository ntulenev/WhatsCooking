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
}
