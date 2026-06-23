using System.Globalization;

using BBRepoList.Models;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Formats user-facing pull request reload summaries.
/// </summary>
internal static class PullRequestReloadSummaryFormatter
{
    /// <summary>
    /// Formats counts of newly loaded pull requests after a reload.
    /// </summary>
    /// <param name="summary">Pull request diff summary.</param>
    /// <returns>User-facing reload summary.</returns>
    public static string Format(PullRequestDiffSummary summary)
    {
        if (!summary.HasNewPullRequests)
        {
            return "No new PRs.";
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Since the last reload, {summary.NewOpenPullRequestsCount} {FormatPlural(summary.NewOpenPullRequestsCount, "new open PR", "new open PRs")} and {summary.NewMergedPullRequestsCount} {FormatPlural(summary.NewMergedPullRequestsCount, "new merged PR", "new merged PRs")} were added.");
    }

    private static string FormatPlural(int count, string singular, string plural) =>
        count == 1 ? singular : plural;
}
