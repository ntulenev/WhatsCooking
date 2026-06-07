using System.Globalization;

using WhatsCooking.Services;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Formats pull request load progress for display.
/// </summary>
internal static class PullRequestLoadProgressFormatter
{
    /// <summary>
    /// Formats progress as a user-facing status message.
    /// </summary>
    public static string Format(PullRequestLoadProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        var stage = progress.Stage switch
        {
            PullRequestLoadStage.LoadingDemoData => "Loading demo data",
            PullRequestLoadStage.Authenticating => "Authenticating",
            PullRequestLoadStage.LoadingRepositories => "Loading repositories",
            PullRequestLoadStage.LoadingOpenPullRequests => "Scanning repositories for open pull requests",
            PullRequestLoadStage.LoadingMergedPullRequests => "Scanning repositories for merged pull requests",
            PullRequestLoadStage.Completed => "Completed",
            _ => throw new ArgumentOutOfRangeException(nameof(progress), progress.Stage, "Unknown pull request load stage.")
        };

        return progress.Completed.HasValue && progress.Total.HasValue
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"{stage}: {progress.Completed.Value}/{progress.Total.Value}")
            : stage;
    }
}
