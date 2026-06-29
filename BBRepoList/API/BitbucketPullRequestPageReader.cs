using BBRepoList.Abstractions;
using BBRepoList.Transport;

namespace BBRepoList.API;

/// <summary>
/// Reads paged Bitbucket pull request DTO responses.
/// </summary>
public sealed class BitbucketPullRequestPageReader : IBitbucketPullRequestPageReader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketPullRequestPageReader"/> class.
    /// </summary>
    /// <param name="transport">Bitbucket transport instance.</param>
    public BitbucketPullRequestPageReader(IBitbucketTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);

        _transport = transport;
    }

    /// <inheritdoc />
    public async Task ForEachAsync(
        Uri initialUrl,
        Func<PullRequestDto, CancellationToken, ValueTask<bool>> handlePullRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(initialUrl);
        ArgumentNullException.ThrowIfNull(handlePullRequest);

        var url = initialUrl;

        while (url is not null)
        {
            var page = await _transport.GetAsync<PullRequestPageDto>(url, cancellationToken).ConfigureAwait(false);
            if (page is null)
            {
                break;
            }

            foreach (var pullRequestDto in page.Values ?? [])
            {
                if (!await handlePullRequest(pullRequestDto, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }
            }

            url = page.Next;
        }
    }

    private readonly IBitbucketTransport _transport;
}
