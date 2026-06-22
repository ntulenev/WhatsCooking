using System.ComponentModel;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Maintains source and filtered rows for one pull request grid.
/// </summary>
internal sealed class PullRequestGridViewState : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestGridViewState"/> class.
    /// </summary>
    /// <param name="getGlobalSearch">Returns the current global search text.</param>
    public PullRequestGridViewState(Func<string> getGlobalSearch)
    {
        ArgumentNullException.ThrowIfNull(getGlobalSearch);

        _getGlobalSearch = getGlobalSearch;
        Filters = new PullRequestFilterState(Refresh);
    }

    /// <summary>
    /// Filtered rows displayed by the grid.
    /// </summary>
    public BulkObservableCollection<PullRequestRow> View { get; } = [];

    /// <summary>
    /// Grid column and reviewed filters.
    /// </summary>
    public PullRequestFilterState Filters { get; }

    /// <summary>
    /// Number of source rows currently loaded.
    /// </summary>
    public int Count => _rows.Count;

    /// <summary>
    /// Replaces the source rows and refreshes the filtered view.
    /// </summary>
    /// <param name="rows">Rows to display.</param>
    public void ReplaceAll(IEnumerable<PullRequestRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        UnsubscribeFromRows();
        _rows.Clear();
        _rows.AddRange(rows);
        SubscribeToRows();
        Refresh();
    }

    /// <summary>
    /// Refreshes the filtered view from the current source rows.
    /// </summary>
    public void Refresh() =>
        View.ReplaceAll(_rows.Where(row => PullRequestRowFilter.Matches(row, _getGlobalSearch(), Filters)));

    /// <summary>
    /// Toggles whether reviewed pull requests are hidden.
    /// </summary>
    public void ToggleReviewedFilter() => Filters.HideReviewed = !Filters.HideReviewed;

    /// <summary>
    /// Clears all filters.
    /// </summary>
    public void ResetFilters() => Filters.Reset();

    /// <summary>
    /// Releases row event subscriptions.
    /// </summary>
    public void Dispose()
    {
        UnsubscribeFromRows();
    }

    private void SubscribeToRows()
    {
        foreach (var row in _rows)
        {
            row.PropertyChanged += OnRowPropertyChanged;
        }
    }

    private void UnsubscribeFromRows()
    {
        foreach (var row in _rows)
        {
            row.PropertyChanged -= OnRowPropertyChanged;
        }
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PullRequestRow.IsReviewed)
            || sender is not PullRequestRow row
            || !Filters.HideReviewed
            || !row.IsReviewed)
        {
            return;
        }

        _ = View.Remove(row);
    }

    private readonly Func<string> _getGlobalSearch;

    private readonly List<PullRequestRow> _rows = [];
}
