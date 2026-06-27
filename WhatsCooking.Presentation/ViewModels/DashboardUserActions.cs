using System.Diagnostics.CodeAnalysis;

using WhatsCooking.Services;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Default implementation of user-triggered dashboard actions.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Facade is created by dependency injection.")]
internal sealed class DashboardUserActions : IDashboardUserActions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardUserActions"/> class.
    /// </summary>
    /// <param name="externalUrlLauncher">External URL launcher.</param>
    /// <param name="aiReviewPromptService">AI review prompt clipboard service.</param>
    public DashboardUserActions(
        IExternalUrlLauncher externalUrlLauncher,
        IAiReviewPromptService aiReviewPromptService)
    {
        ArgumentNullException.ThrowIfNull(externalUrlLauncher);
        ArgumentNullException.ThrowIfNull(aiReviewPromptService);

        _externalUrlLauncher = externalUrlLauncher;
        _aiReviewPromptService = aiReviewPromptService;
    }

    /// <inheritdoc />
    public void OpenUrl(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);

        _externalUrlLauncher.Open(url);
    }

    /// <inheritdoc />
    public string CopyAiReviewPrompt(PullRequestRow pullRequest)
    {
        ArgumentNullException.ThrowIfNull(pullRequest);

        _aiReviewPromptService.CopyPrompt(pullRequest);
        return $"Copied AI review prompt for {pullRequest.RepositoryName} #{pullRequest.PullRequestId}";
    }

    private readonly IExternalUrlLauncher _externalUrlLauncher;

    private readonly IAiReviewPromptService _aiReviewPromptService;
}
