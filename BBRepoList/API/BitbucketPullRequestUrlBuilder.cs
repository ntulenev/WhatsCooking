using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

namespace BBRepoList.API;

/// <summary>
/// Builds Bitbucket pull request endpoint URLs from configured workspace settings.
/// </summary>
public sealed class BitbucketPullRequestUrlBuilder : IBitbucketPullRequestUrlBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketPullRequestUrlBuilder"/> class.
    /// </summary>
    /// <param name="options">Bitbucket configuration options.</param>
    public BitbucketPullRequestUrlBuilder(IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    /// <inheritdoc />
    public Uri CreateOpenPullRequestCountUrl(RepositorySlug repositorySlug) =>
        new(
            $"repositories/{_options.Workspace}/{EscapeRepositorySlug(repositorySlug)}/pullrequests?state=OPEN&pagelen=1&fields=size",
            UriKind.Relative);

    /// <inheritdoc />
    public Uri CreateMergedPullRequestsUrl(RepositorySlug repositorySlug) =>
        new(
            $"repositories/{_options.Workspace}/{EscapeRepositorySlug(repositorySlug)}/pullrequests?state=MERGED&pagelen={_options.PageLen}&sort=-updated_on&fields={EscapeFields(MERGED_PULL_REQUEST_FIELDS)}",
            UriKind.Relative);

    /// <inheritdoc />
    public Uri CreateOpenPullRequestSnapshotsUrl(RepositorySlug repositorySlug) =>
        new(
            $"repositories/{_options.Workspace}/{EscapeRepositorySlug(repositorySlug)}/pullrequests?state=OPEN&pagelen={_options.PageLen}&fields={EscapeFields(OPEN_PULL_REQUEST_SNAPSHOT_FIELDS)}",
            UriKind.Relative);

    private static string EscapeRepositorySlug(RepositorySlug repositorySlug) => Uri.EscapeDataString(repositorySlug.Value);

    private static string EscapeFields(string fields) => Uri.EscapeDataString(fields);

    private readonly BitbucketOptions _options;

    private const string MERGED_PULL_REQUEST_FIELDS =
        "values.id," +
        "values.title," +
        "values.created_on," +
        "values.updated_on," +
        "values.description," +
        "values.summary.raw," +
        "values.author.uuid," +
        "values.author.display_name," +
        "values.participants.user.uuid," +
        "values.participants.state," +
        "values.participants.approved," +
        "next";

    private const string OPEN_PULL_REQUEST_SNAPSHOT_FIELDS =
        "values.id," +
        "values.title," +
        "values.created_on," +
        "values.updated_on," +
        "values.state," +
        "values.description," +
        "values.summary.raw," +
        "values.author.uuid," +
        "values.author.display_name," +
        "values.source.commit.hash," +
        "values.comment_count," +
        "values.task_count," +
        "values.participants.user.uuid," +
        "values.participants.state," +
        "values.participants.approved," +
        "next";
}
