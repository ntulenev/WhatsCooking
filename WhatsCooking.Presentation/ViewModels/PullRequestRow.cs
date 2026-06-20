using BBRepoList.Models;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Pull request row displayed in the dashboard grids.
/// </summary>
internal sealed class PullRequestRow : ObservableObject
{
    /// <summary>
    /// Gets or sets a value indicating whether the pull request has already been reviewed in the dashboard.
    /// </summary>
    public bool IsReviewed {
        get => _isReviewed;
        set => SetProperty(ref _isReviewed, value);
    }

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
    /// Pull request creation timestamp.
    /// </summary>
    public DateTimeOffset OpenedOn { get; }

    /// <summary>
    /// Pull request description text.
    /// </summary>
    public string? DescriptionText { get; }

    /// <summary>
    /// Pull request description length.
    /// </summary>
    public int DescriptionLength { get; }

    /// <summary>
    /// Gets a value indicating whether pull request description should be highlighted as short.
    /// </summary>
    public bool IsDescriptionShort { get; }

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
    /// Gets a value indicating whether missing TTFR should be highlighted as overdue.
    /// </summary>
    public bool IsTtfrAlert { get; }

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
    /// Active request changes count.
    /// </summary>
    public int RequestChangesCount { get; }

    /// <summary>
    /// Gets a value indicating whether pull request has active request changes.
    /// </summary>
    public bool HasRequestChanges => RequestChangesCount > 0;

    /// <summary>
    /// Request changes badge text.
    /// </summary>
    public string RequestChanges { get; }

    /// <summary>
    /// Active approvals count.
    /// </summary>
    public int ApprovalsCount { get; }

    /// <summary>
    /// Gets a value indicating whether pull request has active approvals.
    /// </summary>
    public bool HasApprovals => ApprovalsCount > 0;

    /// <summary>
    /// Approvals badge text.
    /// </summary>
    public string Approvals { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user has commented in activity.
    /// </summary>
    public bool HasCurrentUserDiscussion { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user currently requests changes.
    /// </summary>
    public bool HasCurrentUserRequestChanges { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user currently approves the pull request.
    /// </summary>
    public bool HasCurrentUserApproval { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user has any tracked pull request activity.
    /// </summary>
    public bool HasCurrentUserActivity { get; }

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
    public string SearchText { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestRow"/> class.
    /// </summary>
    /// <param name="number">Row number in the current report table.</param>
    /// <param name="detail">Open pull request details.</param>
    /// <param name="asOf">Timestamp used to calculate relative durations.</param>
    /// <param name="options">Pull request presentation options.</param>
    public PullRequestRow(
        int number,
        PullRequestDetail detail,
        DateTimeOffset asOf,
        PullRequestPresentationOptions options)
    {
        ArgumentNullException.ThrowIfNull(detail, nameof(detail));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        _workspace = options.Workspace;
        Number = number;
        RepositoryName = detail.RepositoryName;
        RepositorySlug = detail.RepositorySlug;
        PullRequestId = detail.PullRequestId.Value;
        Title = detail.Title;
        Author = detail.AuthorDisplayName ?? "-";
        OpenedOn = detail.OpenedOn;
        DescriptionText = detail.DescriptionText;
        DescriptionLength = detail.DescriptionText?.Length ?? 0;
        IsDescriptionShort = detail.HasShortOrMissingDescription(options.MinimalDescriptionLength);
        var openFor = detail.GetOpenDuration(asOf);
        var timeToFirstResponse = detail.TimeToFirstResponse;
        var activityAge = detail.GetLastActivityAge(asOf);
        OpenFor = FormatDuration(openFor);
        OpenForMinutes = openFor.TotalMinutes;
        IsTtfrAlert = timeToFirstResponse is null
                      && openFor > options.TtfrThreshold;
        TimeToFirstResponse = timeToFirstResponse is null && IsTtfrAlert ? "ALERT" : FormatDuration(timeToFirstResponse);
        TimeToFirstResponseMinutes = FormatSortMinutes(timeToFirstResponse);
        ActivityAgeOrMerged = FormatDuration(activityAge);
        ActivityAgeOrMergedMinutes = FormatSortMinutes(activityAge);
        CommentsCount = detail.CommentsCount;
        RequestChangesCount = detail.RequestChangesCount;
        RequestChanges = FormatBadge("RC", detail.RequestChangesCount);
        ApprovalsCount = detail.ApprovalsCount;
        Approvals = FormatBadge("AP", detail.ApprovalsCount);
        HasCurrentUserDiscussion = detail.HasCurrentUserDiscussion;
        HasCurrentUserRequestChanges = detail.HasCurrentUserRequestChanges;
        HasCurrentUserApproval = detail.HasCurrentUserApproval;
        HasCurrentUserActivity = detail.HasCurrentUserActivity;
        CurrentUserActivity = FormatCurrentUserActivity(detail.HasCurrentUserDiscussion, detail.HasCurrentUserRequestChanges, detail.HasCurrentUserApproval);
        SearchText = BuildSearchText();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestRow"/> class.
    /// </summary>
    /// <param name="number">Row number in the current report table.</param>
    /// <param name="pullRequest">Merged pull request details.</param>
    /// <param name="asOf">Timestamp used to calculate relative durations.</param>
    /// <param name="options">Pull request presentation options.</param>
    public PullRequestRow(
        int number,
        MergedPullRequest pullRequest,
        DateTimeOffset asOf,
        PullRequestPresentationOptions options)
    {
        ArgumentNullException.ThrowIfNull(pullRequest, nameof(pullRequest));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        _workspace = options.Workspace;
        Number = number;
        RepositoryName = pullRequest.RepositoryName;
        RepositorySlug = pullRequest.RepositorySlug;
        PullRequestId = pullRequest.PullRequestId.Value;
        Title = pullRequest.Title;
        Author = pullRequest.AuthorDisplayName ?? "-";
        OpenedOn = pullRequest.OpenedOn;
        DescriptionText = pullRequest.DescriptionText;
        DescriptionLength = pullRequest.DescriptionText?.Length ?? 0;
        IsDescriptionShort = pullRequest.HasShortOrMissingDescription(options.MinimalDescriptionLength);
        var openFor = pullRequest.GetOpenDuration();
        var timeToFirstResponse = pullRequest.TimeToFirstResponse;
        var mergedAge = asOf - pullRequest.MergedOn;
        OpenFor = FormatDuration(openFor);
        OpenForMinutes = openFor.TotalMinutes;
        TimeToFirstResponse = FormatDuration(timeToFirstResponse);
        TimeToFirstResponseMinutes = FormatSortMinutes(timeToFirstResponse);
        ActivityAgeOrMerged = FormatDuration(mergedAge);
        ActivityAgeOrMergedMinutes = mergedAge.TotalMinutes;
        CommentsCount = pullRequest.CommentsCount;
        RequestChangesCount = pullRequest.RequestChangesCount;
        RequestChanges = FormatBadge("RC", pullRequest.RequestChangesCount);
        ApprovalsCount = pullRequest.ApprovalsCount;
        Approvals = FormatBadge("AP", pullRequest.ApprovalsCount);
        HasCurrentUserDiscussion = pullRequest.HasCurrentUserDiscussion;
        HasCurrentUserRequestChanges = pullRequest.HasCurrentUserRequestChanges;
        HasCurrentUserApproval = pullRequest.HasCurrentUserApproval;
        HasCurrentUserActivity = pullRequest.HasCurrentUserActivity;
        CurrentUserActivity = FormatCurrentUserActivity(pullRequest.HasCurrentUserDiscussion, pullRequest.HasCurrentUserRequestChanges, pullRequest.HasCurrentUserApproval);
        SearchText = BuildSearchText();
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

    private static Uri BuildRepositoryBrowseUrl(BitbucketWorkspace workspace, RepositorySlug? repositorySlug)
    {
        var encodedWorkspace = Uri.EscapeDataString(workspace.Value);
        var encodedSlug = Uri.EscapeDataString(repositorySlug?.Value ?? string.Empty);

        return new Uri($"https://bitbucket.org/{encodedWorkspace}/{encodedSlug}");
    }

    private static Uri BuildPullRequestUrl(BitbucketWorkspace workspace, RepositorySlug? repositorySlug, int pullRequestId)
    {
        var encodedWorkspace = Uri.EscapeDataString(workspace.Value);
        var encodedSlug = Uri.EscapeDataString(repositorySlug?.Value ?? string.Empty);

        return new Uri($"https://bitbucket.org/{encodedWorkspace}/{encodedSlug}/pull-requests/{pullRequestId}");
    }

    private static string FormatBadge(string label, int count)
    {
        if (count <= 0)
        {
            return "-";
        }
        return $"{label} ({count})";
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

    private string BuildSearchText() =>
        string.Join(" ", Number, RepositoryName, PullRequestId, Title, Author, DescriptionLength, OpenFor, TimeToFirstResponse, ActivityAgeOrMerged, CommentsCount, RequestChanges, Approvals, CurrentUserActivity);

    private readonly BitbucketWorkspace _workspace;

    private bool _isReviewed;

    private RepositorySlug? RepositorySlug { get; }
}
