namespace BBRepoList.Abstractions;

/// <summary>
/// Converts Bitbucket error response bodies into readable messages.
/// </summary>
public interface IBitbucketErrorResponseParser
{
    /// <summary>
    /// Parses an HTTP error response body into a user-readable message fragment.
    /// </summary>
    /// <param name="body">Raw response body.</param>
    /// <param name="mediaType">Response content media type.</param>
    /// <returns>Readable response body text.</returns>
    string Parse(string body, string? mediaType);
}
