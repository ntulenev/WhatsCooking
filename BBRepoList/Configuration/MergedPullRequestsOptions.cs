using System.ComponentModel.DataAnnotations;

namespace BBRepoList.Configuration;

/// <summary>
/// Recently merged pull request report settings.
/// </summary>
public sealed class MergedPullRequestsOptions
{
    /// <summary>
    /// Gets or sets maximum number of concurrent repository requests for merged pull request loading.
    /// </summary>
    [Range(1, 64)]
    public int LoadThreshold { get; init; } = 8;
}
