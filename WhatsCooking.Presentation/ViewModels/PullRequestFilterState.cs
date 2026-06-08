using System.Runtime.CompilerServices;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Pull request grid filter state.
/// </summary>
internal sealed class PullRequestFilterState : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestFilterState"/> class.
    /// </summary>
    /// <param name="onChanged">Action invoked when any filter value changes.</param>
    public PullRequestFilterState(Action onChanged)
    {
        ArgumentNullException.ThrowIfNull(onChanged);

        _onChanged = onChanged;
    }

    /// <summary>
    /// Filter for the row number column.
    /// </summary>
    public string Number {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the repository column.
    /// </summary>
    public string Repository {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the pull request column.
    /// </summary>
    public string PullRequest {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the author column.
    /// </summary>
    public string Author {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the description length column.
    /// </summary>
    public string DescriptionLength {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the open duration column.
    /// </summary>
    public string OpenFor {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the time to first response column.
    /// </summary>
    public string TimeToFirstResponse {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the latest activity or merge age column.
    /// </summary>
    public string Activity {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the comments column.
    /// </summary>
    public string Comments {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the request changes column.
    /// </summary>
    public string RequestChanges {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the approvals column.
    /// </summary>
    public string Approvals {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the current user activity column.
    /// </summary>
    public string CurrentUserActivity {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Clears all filter values.
    /// </summary>
    public void Reset()
    {
        Number = string.Empty;
        Repository = string.Empty;
        PullRequest = string.Empty;
        Author = string.Empty;
        DescriptionLength = string.Empty;
        OpenFor = string.Empty;
        TimeToFirstResponse = string.Empty;
        Activity = string.Empty;
        Comments = string.Empty;
        RequestChanges = string.Empty;
        Approvals = string.Empty;
        CurrentUserActivity = string.Empty;
    }

    private bool SetFilterProperty(
        ref string field,
        string value,
        [CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName))
        {
            return false;
        }

        _onChanged();
        return true;
    }

    private readonly Action _onChanged;
}
