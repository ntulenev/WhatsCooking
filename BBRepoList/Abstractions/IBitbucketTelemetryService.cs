using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Collects and exposes Bitbucket API request telemetry for the current run.
/// </summary>
public interface IBitbucketTelemetryService
{
    /// <summary>
    /// Clears all tracked requests for a new load operation.
    /// </summary>
    void Reset();

    /// <summary>
    /// Tracks a Bitbucket API request.
    /// </summary>
    /// <param name="requestUri">Request URI.</param>
    void TrackRequest(Uri requestUri);

    /// <summary>
    /// Returns the telemetry snapshot for the current run.
    /// </summary>
    /// <returns>Current telemetry snapshot.</returns>
    BitbucketTelemetrySnapshot GetSnapshot();
}
