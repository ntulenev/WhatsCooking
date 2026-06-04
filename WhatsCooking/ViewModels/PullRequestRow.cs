using BBRepoList.Configuration;
using BBRepoList.Models;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Pull request row displayed in the dashboard grids.
/// </summary>
internal sealed class PullRequestRow
{
    /// <summary>
    /// Row number in the current report table.
    /// </summary>
    public int Number { get; }

    /// <summary>
    /// Repository display name.
    /// </summary>
    public string RepositoryName { get; }

    /// <summary>
    /// Pull request identifier in repository scope.
    /// </summary>
    public int PullRequestId { get; }

    /// <summary>
    /// Pull request title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Combined pull request identifier and title.
    /// </summary>
    public string PullRequestDisplay => $"#{PullRequestId} {Title}";

    /// <summary>
    /// Pull request author display name.
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// Pull request description length.
    /// </summary>
    public int DescriptionLength { get; }

    /// <summary>
    /// Formatted pull request open duration.
    /// </summary>
    public string OpenFor { get; }

    /// <summary>
    /// Pull request open duration in minutes for sorting.
    /// </summary>
    public double OpenForMinutes { get; }

    /// <summary>
    /// Formatted time to first response.
    /// </summary>
    public string TimeToFirstResponse { get; }

    /// <summary>
    /// Time to first response in minutes for sorting.
    /// </summary>
    public double TimeToFirstResponseMinutes { get; }

    /// <summary>
    /// Formatted latest activity age or merged age.
    /// </summary>
    public string ActivityAgeOrMerged { get; }

    /// <summary>
    /// Latest activity age or merged age in minutes for sorting.
    /// </summary>
    public double ActivityAgeOrMergedMinutes { get; }

    /// <summary>
    /// Pull request comments count.
    /// </summary>
    public int CommentsCount { get; }

    /// <summary>
    /// Request changes badge text.
    /// </summary>
    public string RequestChanges { get; }

    /// <summary>
    /// Approvals badge text.
    /// </summary>
    public string Approvals { get; }

    /// <summary>
    /// Current authenticated user activity summary.
    /// </summary>
    public string CurrentUserActivity { get; }

    /// <summary>
    /// Bitbucket repository URL.
    /// </summary>
    public Uri RepositoryUrl => BuildRepositoryBrowseUrl(_workspace, RepositorySlug);

    /// <summary>
    /// Bitbucket pull request URL.
    /// </summary>
    public Uri PullRequestUrl => BuildPullRequestUrl(_workspace, RepositorySlug, PullRequestId);

    /// <summary>
    /// Combined searchable row text.
    /// </summary>
    public string SearchText => string.Join(" ", Number, RepositoryName, PullRequestId, Title, Author, DescriptionLength, OpenFor, TimeToFirstResponse, ActivityAgeOrMerged, CommentsCount, RequestChanges, Approvals, CurrentUserActivity);

    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestRow"/> class.
    /// </summary>
    /// <param name="number">Row number in the current report table.</param>
    /// <param name="detail">Open pull request details.</param>
    /// <param name="asOf">Timestamp used to calculate relative durations.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public PullRequestRow(int number, PullRequestDetail detail, DateTimeOffset asOf, BitbucketOptions options)
    {
        ArgumentNullException.ThrowIfNull(detail, nameof(detail));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        _workspace = options.Workspace;
        Number = number;
        RepositoryName = detail.RepositoryName;
        RepositorySlug = detail.RepositorySlug ?? string.Empty;
        PullRequestId = detail.PullRequestId;
        Title = detail.Title;
        Author = detail.AuthorDisplayName ?? "-";
        DescriptionLength = detail.DescriptionText?.Length ?? 0;
        var openFor = detail.GetOpenDuration(asOf);
        var timeToFirstResponse = detail.TimeToFirstResponse;
        var activityAge = detail.GetLastActivityAge(asOf);
        OpenFor = FormatDuration(openFor);
        OpenForMinutes = openFor.TotalMinutes;
        TimeToFirstResponse = FormatDuration(timeToFirstResponse);
        TimeToFirstResponseMinutes = FormatSortMinutes(timeToFirstResponse);
        ActivityAgeOrMerged = FormatDuration(activityAge);
        ActivityAgeOrMergedMinutes = FormatSortMinutes(activityAge);
        CommentsCount = detail.CommentsCount;
        RequestChanges = FormatBadge("RC", detail.RequestChangesCount, detail.HasCurrentUserRequestChanges);
        Approvals = FormatBadge("AP", detail.ApprovalsCount, detail.HasCurrentUserApproval);
        CurrentUserActivity = FormatCurrentUserActivity(detail.HasCurrentUserDiscussion, detail.HasCurrentUserRequestChanges, detail.HasCurrentUserApproval);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestRow"/> class.
    /// </summary>
    /// <param name="number">Row number in the current report table.</param>
    /// <param name="pullRequest">Merged pull request details.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public PullRequestRow(int number, MergedPullRequest pullRequest, BitbucketOptions options)
    {
        ArgumentNullException.ThrowIfNull(pullRequest, nameof(pullRequest));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        _workspace = options.Workspace;
        Number = number;
        RepositoryName = pullRequest.RepositoryName;
        RepositorySlug = pullRequest.RepositorySlug ?? string.Empty;
        PullRequestId = pullRequest.PullRequestId;
        Title = pullRequest.Title;
        Author = pullRequest.AuthorDisplayName ?? "-";
        DescriptionLength = pullRequest.DescriptionText?.Length ?? 0;
        var openFor = pullRequest.GetOpenDuration();
        var timeToFirstResponse = pullRequest.TimeToFirstResponse;
        var mergedAge = DateTimeOffset.Now - pullRequest.MergedOn;
        OpenFor = FormatDuration(openFor);
        OpenForMinutes = openFor.TotalMinutes;
        TimeToFirstResponse = FormatDuration(timeToFirstResponse);
        TimeToFirstResponseMinutes = FormatSortMinutes(timeToFirstResponse);
        ActivityAgeOrMerged = FormatDuration(mergedAge);
        ActivityAgeOrMergedMinutes = mergedAge.TotalMinutes;
        CommentsCount = pullRequest.CommentsCount;
        RequestChanges = FormatBadge("RC", pullRequest.RequestChangesCount, pullRequest.HasCurrentUserRequestChanges);
        Approvals = FormatBadge("AP", pullRequest.ApprovalsCount, pullRequest.HasCurrentUserApproval);
        CurrentUserActivity = FormatCurrentUserActivity(pullRequest.HasCurrentUserDiscussion, pullRequest.HasCurrentUserRequestChanges, pullRequest.HasCurrentUserApproval);
    }

    private static string FormatDuration(TimeSpan? duration)
    {
        if (!duration.HasValue)
        {
            return "-";
        }
        var value = duration.Value;
        if (value.TotalDays >= 1)
        {
            return $"{(int)value.TotalDays}d {value.Hours}h {value.Minutes}m";
        }
        if (value.TotalHours >= 1)
        {
            return $"{(int)value.TotalHours}h {value.Minutes}m";
        }
        return $"{Math.Max((int)value.TotalMinutes, 0)}m";
    }

    private static double FormatSortMinutes(TimeSpan? duration) => duration?.TotalMinutes ?? -1;

    private static Uri BuildRepositoryBrowseUrl(string workspace, string repositorySlug)
    {
        var encodedWorkspace = Uri.EscapeDataString(workspace.Trim());
        var encodedSlug = Uri.EscapeDataString(repositorySlug.Trim());

        return new Uri($"https://bitbucket.org/{encodedWorkspace}/{encodedSlug}");
    }

    private static Uri BuildPullRequestUrl(string workspace, string repositorySlug, int pullRequestId)
    {
        var encodedWorkspace = Uri.EscapeDataString(workspace.Trim());
        var encodedSlug = Uri.EscapeDataString(repositorySlug.Trim());

        return new Uri($"https://bitbucket.org/{encodedWorkspace}/{encodedSlug}/pull-requests/{pullRequestId}");
    }

    private static string FormatBadge(string label, int count, bool isCurrentUser)
    {
        if (count <= 0)
        {
            return "-";
        }
        return isCurrentUser ? $"{label} ({count}) me" : $"{label} ({count})";
    }

    private static string FormatCurrentUserActivity(bool hasDiscussion, bool hasRequestChanges, bool hasApproval)
    {
        if (hasRequestChanges)
        {
            return "Request changes";
        }
        if (hasApproval)
        {
            return "Approval";
        }
        if (hasDiscussion)
        {
            return "Comment";
        }
        return "-";
    }

    private readonly string _workspace;

    private string RepositorySlug { get; }
}
