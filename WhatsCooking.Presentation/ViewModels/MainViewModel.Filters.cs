using System.ComponentModel;

namespace WhatsCooking.ViewModels;

internal sealed partial class MainViewModel
{
    /// <summary>
    /// Global text filter applied to pull request tables.
    /// </summary>
    public string GlobalSearch {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                SchedulePullRequestFilterRefresh();
            }
        }
    } = string.Empty;

    /// <summary>
    /// Filter for the merged pull request row number column.
    /// </summary>
    public string MergedNumberFilter {
        get => MergedPullRequestFilters.Number;
        set => MergedPullRequestFilters.Number = value;
    }

    /// <summary>
    /// Filter for the merged pull request repository column.
    /// </summary>
    public string MergedRepositoryFilter {
        get => MergedPullRequestFilters.Repository;
        set => MergedPullRequestFilters.Repository = value;
    }

    /// <summary>
    /// Filter for the merged pull request column.
    /// </summary>
    public string MergedPullRequestFilter {
        get => MergedPullRequestFilters.PullRequest;
        set => MergedPullRequestFilters.PullRequest = value;
    }

    /// <summary>
    /// Filter for the merged pull request author column.
    /// </summary>
    public string MergedAuthorFilter {
        get => MergedPullRequestFilters.Author;
        set => MergedPullRequestFilters.Author = value;
    }

    /// <summary>
    /// Filter for the merged pull request description length column.
    /// </summary>
    public string MergedDescriptionLengthFilter {
        get => MergedPullRequestFilters.DescriptionLength;
        set => MergedPullRequestFilters.DescriptionLength = value;
    }

    /// <summary>
    /// Filter for the merged pull request TTFR column.
    /// </summary>
    public string MergedTimeToFirstResponseFilter {
        get => MergedPullRequestFilters.TimeToFirstResponse;
        set => MergedPullRequestFilters.TimeToFirstResponse = value;
    }

    /// <summary>
    /// Filter for the merged pull request merge age column.
    /// </summary>
    public string MergedActivityFilter {
        get => MergedPullRequestFilters.Activity;
        set => MergedPullRequestFilters.Activity = value;
    }

    /// <summary>
    /// Filter for the merged pull request comments column.
    /// </summary>
    public string MergedCommentsFilter {
        get => MergedPullRequestFilters.Comments;
        set => MergedPullRequestFilters.Comments = value;
    }

    /// <summary>
    /// Filter for the merged pull request changes column.
    /// </summary>
    public string MergedRequestChangesFilter {
        get => MergedPullRequestFilters.RequestChanges;
        set => MergedPullRequestFilters.RequestChanges = value;
    }

    /// <summary>
    /// Filter for the merged pull request approvals column.
    /// </summary>
    public string MergedApprovalsFilter {
        get => MergedPullRequestFilters.Approvals;
        set => MergedPullRequestFilters.Approvals = value;
    }

    /// <summary>
    /// Filter for the merged pull request current user activity column.
    /// </summary>
    public string MergedCurrentUserActivityFilter {
        get => MergedPullRequestFilters.CurrentUserActivity;
        set => MergedPullRequestFilters.CurrentUserActivity = value;
    }

    /// <summary>
    /// Open pull request grid filters.
    /// </summary>
    public PullRequestFilterState OpenPullRequestFilters { get; }

    /// <summary>
    /// Recently merged pull request grid filters.
    /// </summary>
    public PullRequestFilterState MergedPullRequestFilters { get; }

    /// <summary>
    /// Text displayed by the open pull request reviewed filter button.
    /// </summary>
    public string OpenReviewedFilterButtonText =>
        OpenPullRequestFilters.HideReviewed ? "Show all" : "Hide reviewed";

    /// <summary>
    /// Text displayed by the merged pull request reviewed filter button.
    /// </summary>
    public string MergedReviewedFilterButtonText =>
        MergedPullRequestFilters.HideReviewed ? "Show all" : "Hide reviewed";

    /// <summary>
    /// Gets a value indicating whether reviewed open pull requests are hidden.
    /// </summary>
    public bool IsOpenReviewedFilterActive => OpenPullRequestFilters.HideReviewed;

    /// <summary>
    /// Gets a value indicating whether reviewed merged pull requests are hidden.
    /// </summary>
    public bool IsMergedReviewedFilterActive => MergedPullRequestFilters.HideReviewed;

    private void RefreshViews()
    {
        _dashboardState.Refresh();
    }

    private void SchedulePullRequestFilterRefresh() => RefreshViews();

    private void ToggleOpenReviewedFilter() => _dashboardState.OpenPullRequests.ToggleReviewedFilter();

    private void ToggleMergedReviewedFilter() => _dashboardState.MergedPullRequests.ToggleReviewedFilter();

    private void OnOpenPullRequestFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PullRequestFilterState.HideReviewed))
        {
            OnPropertyChanged(nameof(OpenReviewedFilterButtonText));
            OnPropertyChanged(nameof(IsOpenReviewedFilterActive));
        }
    }

    private void OnMergedPullRequestFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PullRequestFilterState.HideReviewed))
        {
            OnPropertyChanged(nameof(MergedReviewedFilterButtonText));
            OnPropertyChanged(nameof(IsMergedReviewedFilterActive));
        }
    }

    private void ResetFilters()
    {
        GlobalSearch = string.Empty;
        _dashboardState.ResetFilters();
        RaiseMergedFilterPropertiesChanged();
        TelemetryDashboard.ResetFilter();
    }

    private void RaiseMergedFilterPropertiesChanged()
    {
        OnPropertyChanged(nameof(MergedNumberFilter));
        OnPropertyChanged(nameof(MergedRepositoryFilter));
        OnPropertyChanged(nameof(MergedPullRequestFilter));
        OnPropertyChanged(nameof(MergedAuthorFilter));
        OnPropertyChanged(nameof(MergedDescriptionLengthFilter));
        OnPropertyChanged(nameof(MergedTimeToFirstResponseFilter));
        OnPropertyChanged(nameof(MergedActivityFilter));
        OnPropertyChanged(nameof(MergedCommentsFilter));
        OnPropertyChanged(nameof(MergedRequestChangesFilter));
        OnPropertyChanged(nameof(MergedApprovalsFilter));
        OnPropertyChanged(nameof(MergedCurrentUserActivityFilter));
    }
}
