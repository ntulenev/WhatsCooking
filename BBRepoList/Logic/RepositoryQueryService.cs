using BBRepoList.Abstractions;
using BBRepoList.Models;

namespace BBRepoList.Logic;

/// <summary>
/// Loads repositories from Bitbucket with optional name filtering.
/// </summary>
public sealed class RepositoryQueryService : IRepositoryQueryService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryQueryService"/> class.
    /// </summary>
    /// <param name="api">Bitbucket repository API client.</param>
    public RepositoryQueryService(IBitbucketRepoApiClient api)
    {
        ArgumentNullException.ThrowIfNull(api);

        _api = api;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Repository>> GetRepositoriesAsync(
        FilterPattern filterPattern,
        IProgress<RepoLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var matchedRepositories = new List<Repository>();

        var seen = 0;
        var matched = 0;

        await foreach (var repository in _api.GetRepositoriesAsync(filterPattern, cancellationToken).ConfigureAwait(false))
        {
            seen++;

            if (filterPattern.Filter(repository))
            {
                matched++;
                matchedRepositories.Add(repository);
            }

            progress?.Report(new RepoLoadProgress(seen, matched));
        }

        return matchedRepositories;
    }

    private readonly IBitbucketRepoApiClient _api;
}
