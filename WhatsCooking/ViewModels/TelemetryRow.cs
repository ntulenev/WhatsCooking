using System.Globalization;

using BBRepoList.Models;

namespace WhatsCooking.ViewModels;

/// <summary>
/// Bitbucket API telemetry row displayed in the telemetry grid.
/// </summary>
internal sealed class TelemetryRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryRow"/> class.
    /// </summary>
    /// <param name="number">Row number in the telemetry table.</param>
    /// <param name="statistic">Bitbucket API request statistic.</param>
    /// <param name="totalRequests">Total captured Bitbucket API request count.</param>
    public TelemetryRow(int number, BitbucketApiRequestStatistic statistic, int totalRequests)
    {
        ArgumentNullException.ThrowIfNull(statistic);

        Number = number;
        ApiName = statistic.ApiName;
        RequestCount = statistic.RequestCount;
        Share = totalRequests > 0 ? (double)statistic.RequestCount / totalRequests : 0;
        ShareText = Share.ToString("P1", CultureInfo.InvariantCulture);
        SearchText = string.Join(" ", Number, ApiName, RequestCount, ShareText);
    }

    /// <summary>
    /// Row number in the telemetry table.
    /// </summary>
    public int Number { get; }

    /// <summary>
    /// Bitbucket API name.
    /// </summary>
    public string ApiName { get; }

    /// <summary>
    /// Request count captured for the API.
    /// </summary>
    public int RequestCount { get; }

    /// <summary>
    /// Request share relative to all captured Bitbucket API calls.
    /// </summary>
    public double Share { get; }

    /// <summary>
    /// Formatted request share.
    /// </summary>
    public string ShareText { get; }

    /// <summary>
    /// Combined searchable row text.
    /// </summary>
    public string SearchText { get; }
}
