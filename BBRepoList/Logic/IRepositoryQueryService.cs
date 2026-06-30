using BBRepoList.Models;

namespace BBRepoList.Logic;

/// <summary>
/// Loads and filters repositories for repository reports.
/// </summary>
public interface IRepositoryQueryService
{
    /// <summary>
    /// Loads repositories that match the provided filter.
    /// </summary>
    /// <param name="filterPattern">Repository filter pattern.</param>
    /// <param name="progress">Optional repository load progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matched repositories.</returns>
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync(
        FilterPattern filterPattern,
        IProgress<RepoLoadProgress>? progress,
        CancellationToken cancellationToken);
}
