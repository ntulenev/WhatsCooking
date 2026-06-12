using WhatsCooking.ViewModels;

namespace WhatsCooking.Services;

/// <summary>
/// Creates and copies an AI pull request review prompt.
/// </summary>
internal interface IAiReviewPromptService
{
    /// <summary>
    /// Copies an AI review prompt for the pull request.
    /// </summary>
    /// <param name="pullRequest">Pull request row to review.</param>
    void CopyPrompt(PullRequestRow pullRequest);
}
