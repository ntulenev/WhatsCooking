namespace WhatsCooking.Services;

/// <summary>
/// Progress state reported while loading pull request dashboard data.
/// </summary>
/// <param name="Stage">Current loading stage.</param>
/// <param name="Completed">Completed work item count.</param>
/// <param name="Total">Total work item count.</param>
internal sealed record PullRequestLoadProgress(string Stage, int? Completed = null, int? Total = null)
{
    /// <summary>
    /// User-facing progress message.
    /// </summary>
    public string Message {
        get
        {
            if (Completed.HasValue && Total.HasValue)
            {
                return $"{Stage}: {Completed.Value}/{Total.Value}";
            }
            return Stage;
        }
    }
}
