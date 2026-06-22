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

    /// <summary>
    /// Validates user input and returns either the parsed days or a user-facing error.
    /// </summary>
    /// <param name="value">User-entered period.</param>
    /// <returns>Validation result.</returns>
    public static MergedPullRequestPeriodValidation Validate(string value) =>
        TryParse(value, out var days)
            ? MergedPullRequestPeriodValidation.Valid(days)
            : MergedPullRequestPeriodValidation.Invalid(
                $"Enter a whole number from {MINIMUM_DAYS} to {MAXIMUM_DAYS}.");
}

/// <summary>
/// Result of validating a recently merged pull request period.
/// </summary>
/// <param name="IsValid">Whether the input is valid.</param>
/// <param name="Days">Parsed days when valid.</param>
/// <param name="Error">User-facing validation error when invalid.</param>
internal readonly record struct MergedPullRequestPeriodValidation(bool IsValid, int Days, string? Error)
{
    /// <summary>
    /// Creates a valid result.
    /// </summary>
    /// <param name="days">Parsed days.</param>
    /// <returns>Valid result.</returns>
    public static MergedPullRequestPeriodValidation Valid(int days) => new(true, days, null);

    /// <summary>
    /// Creates an invalid result.
    /// </summary>
    /// <param name="error">User-facing validation error.</param>
    /// <returns>Invalid result.</returns>
    public static MergedPullRequestPeriodValidation Invalid(string error) => new(false, 0, error);
}
