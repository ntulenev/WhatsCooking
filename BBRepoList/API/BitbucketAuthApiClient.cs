using System.Net.Http.Headers;
using System.Text;

using BBRepoList.Abstractions;
using BBRepoList.Models;
using BBRepoList.Transport;

namespace BBRepoList.API;

/// <summary>
/// Bitbucket REST API client implementation for authentication operations.
/// </summary>
public sealed class BitbucketAuthApiClient : IBitbucketAuthApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketAuthApiClient"/> class.
    /// </summary>
    /// <param name="transport">Bitbucket transport instance.</param>
    public BitbucketAuthApiClient(IBitbucketTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);

        _transport = transport;
    }

    /// <inheritdoc />
    public async Task<BitbucketUser> AuthSelfCheckAsync(CancellationToken cancellationToken)
    {
        var dto = await _transport
            .GetAsync<BitbucketUserDto>(new Uri("user", UriKind.Relative), cancellationToken)
            .ConfigureAwait(false);
        return dto is null ? throw new InvalidOperationException("Bitbucket user response is empty.") : dto.ToDomain();
    }

    /// <inheritdoc />
    AuthenticationHeaderValue IBitbucketAuthApiClient.BuildAuthHeader(string authEmail, string authApiToken)
    {
        ArgumentNullException.ThrowIfNull(authEmail);
        ArgumentNullException.ThrowIfNull(authApiToken);

        return BuildAuthHeader(authEmail, authApiToken);
    }

    private static AuthenticationHeaderValue BuildAuthHeader(string authEmail, string authApiToken)
    {
        var raw = $"{authEmail}:{authApiToken}";
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        return new AuthenticationHeaderValue("Basic", b64);
    }

    private readonly IBitbucketTransport _transport;
}
