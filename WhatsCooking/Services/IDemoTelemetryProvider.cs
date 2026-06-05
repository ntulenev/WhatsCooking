using BBRepoList.Models;

namespace WhatsCooking.Services;

/// <summary>
/// Creates synthetic Bitbucket telemetry for demo mode.
/// </summary>
internal interface IDemoTelemetryProvider
{
    /// <summary>
    /// Creates demo telemetry data.
    /// </summary>
    /// <returns>Demo telemetry snapshot.</returns>
    BitbucketTelemetrySnapshot Create();
}
