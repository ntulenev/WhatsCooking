using System.Globalization;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Matches pull request rows against global and column filters.
/// </summary>
internal static class PullRequestRowFilter
{
    /// <summary>
    /// Returns whether the row matches all active filters.
    /// </summary>
    public static bool Matches(
        PullRequestRow row,
        string globalSearch,
        PullRequestFilterState filters)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(filters);

        return Contains(row.SearchText, globalSearch)
            && Contains(row.Number.ToString(CultureInfo.InvariantCulture), filters.Number)
            && Contains(row.RepositoryName, filters.Repository)
            && Contains(row.PullRequestDisplay, filters.PullRequest)
            && Contains(row.Author, filters.Author)
            && Contains(row.DescriptionLength.ToString(CultureInfo.InvariantCulture), filters.DescriptionLength)
            && Contains(row.OpenFor, filters.OpenFor)
            && Contains(row.TimeToFirstResponse, filters.TimeToFirstResponse)
            && Contains(row.ActivityAgeOrMerged, filters.Activity)
            && Contains(row.CommentsCount.ToString(CultureInfo.InvariantCulture), filters.Comments)
            && Contains(row.RequestChanges, filters.RequestChanges)
            && Contains(row.Approvals, filters.Approvals)
            && Contains(row.CurrentUserActivity, filters.CurrentUserActivity);
    }

    private static bool Contains(string source, string filter) =>
        string.IsNullOrWhiteSpace(filter)
        || source.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);
}
