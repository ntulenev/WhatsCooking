using System.Globalization;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Parses and validates the recently merged pull request period.
/// </summary>
internal static class MergedPullRequestPeriod
{
    /// <summary>
    /// Minimum supported period in days.
    /// </summary>
    public const int MINIMUM_DAYS = 1;

    /// <summary>
    /// Maximum supported period in days.
    /// </summary>
    public const int MAXIMUM_DAYS = 365;

    /// <summary>
    /// Parses a valid period from user input.
    /// </summary>
    /// <param name="value">User-entered period.</param>
    /// <param name="days">Parsed period when successful.</param>
    /// <returns><see langword="true"/> when the input is a supported whole number.</returns>
    public static bool TryParse(string value, out int days) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out days)
        && days is >= MINIMUM_DAYS and <= MAXIMUM_DAYS;
}
