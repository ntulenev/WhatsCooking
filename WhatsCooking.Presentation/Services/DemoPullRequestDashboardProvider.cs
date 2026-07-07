using System.Diagnostics.CodeAnalysis;

using BBRepoList.Models;

namespace WhatsCooking.Services;

/// <summary>
/// Creates synthetic pull request dashboard data for demo mode.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Provider is created by dependency injection.")]
internal sealed class DemoPullRequestDashboardProvider : IDemoPullRequestDashboardProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DemoPullRequestDashboardProvider"/> class.
    /// </summary>
    /// <param name="timeProvider">Time provider used to anchor relative demo dates.</param>
    public DemoPullRequestDashboardProvider(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Creates demo repositories, open pull requests and recently merged pull requests.
    /// </summary>
    /// <returns>Demo pull request dashboard data.</returns>
    public PullRequestLoadResult Create()
    {
        var asOf = _timeProvider.GetLocalNow();
        List<Repository> repositories =
        [
            CreateDemoRepository("Customer Portal", "customer-portal", 2, asOf),
            CreateDemoRepository("Billing Service", "billing-service", 1, asOf),
            CreateDemoRepository("Identity Gateway", "identity-gateway", 1, asOf),
            CreateDemoRepository("Reporting API", "reporting-api", 2, asOf),
            CreateDemoRepository("Mobile Backend", "mobile-backend", 1, asOf),
            CreateDemoRepository("Notification Worker", "notification-worker", 1, asOf),
            CreateDemoRepository("Admin Console", "admin-console", 2, asOf),
            CreateDemoRepository("Data Importer", "data-importer", 1, asOf),
            CreateDemoRepository("Search Indexer", "search-indexer", 1, asOf),
            CreateDemoRepository("Payments Adapter", "payments-adapter", 1, asOf)
        ];

        List<PullRequestDetail> openPullRequests =
        [
            CreateOpenDemoPullRequest(repositories[0], 1842, "Add saved filters to account activity grid", asOf.AddHours(-2.5), "Maya Ortiz", asOf.AddHours(-1.8), asOf.AddMinutes(-22), true, DEMO_DESCRIPTION_LONG, 7, 0, false, 2, false),
            CreateOpenDemoPullRequest(repositories[6], 517, "Refactor permission checks for tenant-scoped admin pages", asOf.AddHours(-7), "Ethan Brooks", asOf.AddHours(-5.5), asOf.AddMinutes(-50), false, DEMO_DESCRIPTION_LONG, 11, 1, true, 1, false),
            CreateOpenDemoPullRequest(repositories[1], 2291, "Fix retry metadata on invoice reconciliation job", asOf.AddHours(-11), "Priya Shah", asOf.AddHours(-9.5), asOf.AddHours(-2), true, DEMO_DESCRIPTION_LONG, 4, 0, false, 3, true),
            CreateOpenDemoPullRequest(repositories[3], 1438, "Expose export progress through report status endpoint", asOf.AddHours(-18), "Noah Stein", null, asOf.AddHours(-6), false, "Initial implementation.", 2, 0, false, 0, false),
            CreateOpenDemoPullRequest(repositories[4], 805, "Normalize device capability payload before caching", asOf.AddDays(-1.2), "Sofia Nguyen", asOf.AddDays(-1).AddHours(2), asOf.AddHours(-4), true, DEMO_DESCRIPTION_LONG, 9, 2, false, 1, false),
            CreateOpenDemoPullRequest(repositories[2], 662, "Add audit trail for external login callbacks", asOf.AddDays(-1.8), "Liam Chen", asOf.AddDays(-1.6), asOf.AddHours(-9), false, DEMO_DESCRIPTION_LONG, 6, 0, false, 2, false),
            CreateOpenDemoPullRequest(repositories[5], 331, "Throttle notification fan-out during incident windows", asOf.AddDays(-2.1), "Ava Martin", asOf.AddDays(-2).AddHours(4), asOf.AddHours(-13), true, DEMO_DESCRIPTION_LONG, 13, 1, false, 0, false),
            CreateOpenDemoPullRequest(repositories[8], 94, "Rebuild stale search documents after taxonomy changes", asOf.AddDays(-2.7), "Daniel Kim", asOf.AddDays(-2.4), asOf.AddDays(-1.1), false, DEMO_DESCRIPTION_LONG, 5, 0, false, 1, false),
            CreateOpenDemoPullRequest(repositories[9], 1206, "Support split authorization for partial refunds", asOf.AddDays(-3.2), "Nina Patel", asOf.AddDays(-3).AddHours(8), asOf.AddHours(-17), true, DEMO_DESCRIPTION_LONG, 15, 2, true, 2, false),
            CreateOpenDemoPullRequest(repositories[7], 408, "Validate imported column mappings before preview", asOf.AddDays(-4.1), "Oliver Reed", null, asOf.AddDays(-2.8), false, "Draft.", 1, 0, false, 0, false)
        ];
        AddOpenDemoPullRequestVariants(openPullRequests);

        List<MergedPullRequest> mergedPullRequests =
        [
            CreateMergedDemoPullRequest(repositories[3], 1432, "Cache generated report manifests for repeat downloads", asOf.AddDays(-2.8), "Maya Ortiz", asOf.AddDays(-2.6), asOf.AddHours(-3), true, asOf.AddHours(-2), DEMO_DESCRIPTION_LONG, 8, 0, false, 3, true),
            CreateMergedDemoPullRequest(repositories[0], 1838, "Preserve selected date range when switching accounts", asOf.AddDays(-3.1), "Ethan Brooks", asOf.AddDays(-3), asOf.AddHours(-5), false, asOf.AddHours(-4), DEMO_DESCRIPTION_LONG, 6, 0, false, 2, false),
            CreateMergedDemoPullRequest(repositories[6], 512, "Add keyboard navigation to role assignment dialog", asOf.AddDays(-4.5), "Sofia Nguyen", asOf.AddDays(-4.2), asOf.AddHours(-8), true, asOf.AddHours(-7), DEMO_DESCRIPTION_LONG, 10, 0, false, 2, true),
            CreateMergedDemoPullRequest(repositories[1], 2285, "Reduce lock contention in settlement batch writer", asOf.AddDays(-5.3), "Liam Chen", asOf.AddDays(-5), asOf.AddHours(-12), false, asOf.AddHours(-11), DEMO_DESCRIPTION_LONG, 12, 1, false, 3, false),
            CreateMergedDemoPullRequest(repositories[5], 326, "Move email template rendering behind queue boundary", asOf.AddDays(-5.9), "Noah Stein", asOf.AddDays(-5.7), asOf.AddHours(-16), true, asOf.AddHours(-15), DEMO_DESCRIPTION_LONG, 4, 0, false, 1, false),
            CreateMergedDemoPullRequest(repositories[4], 799, "Add telemetry for mobile session refresh failures", asOf.AddDays(-6.4), "Priya Shah", asOf.AddDays(-6.2), asOf.AddDays(-1), false, asOf.AddHours(-19), DEMO_DESCRIPTION_LONG, 9, 0, false, 2, false),
            CreateMergedDemoPullRequest(repositories[2], 657, "Handle expired SSO assertions with clear login state", asOf.AddDays(-7.1), "Nina Patel", asOf.AddDays(-6.9), asOf.AddDays(-1.4), true, asOf.AddDays(-1.1), DEMO_DESCRIPTION_LONG, 7, 0, false, 3, true),
            CreateMergedDemoPullRequest(repositories[8], 89, "Compact index rebuild batches by content type", asOf.AddDays(-8.2), "Daniel Kim", asOf.AddDays(-8), asOf.AddDays(-1.8), false, asOf.AddDays(-1.5), DEMO_DESCRIPTION_LONG, 5, 0, false, 1, false),
            CreateMergedDemoPullRequest(repositories[9], 1198, "Record gateway reference IDs for dispute lookup", asOf.AddDays(-9.3), "Ava Martin", asOf.AddDays(-9), asOf.AddDays(-2.2), true, asOf.AddDays(-2), DEMO_DESCRIPTION_LONG, 11, 0, false, 2, false),
            CreateMergedDemoPullRequest(repositories[7], 402, "Improve CSV preview error grouping", asOf.AddDays(-10.5), "Oliver Reed", asOf.AddDays(-10.2), asOf.AddDays(-3), false, asOf.AddDays(-2.6), DEMO_DESCRIPTION_LONG, 3, 0, false, 1, false)
        ];
        AddMergedDemoPullRequestVariants(mergedPullRequests);
        UpdateDemoRepositoryOpenPullRequestCounts(repositories, openPullRequests);

        return new PullRequestLoadResult(repositories, openPullRequests, mergedPullRequests);
    }

    private static Repository CreateDemoRepository(string name, string slug, int openPullRequestsCount, DateTimeOffset asOf)
    {
        var repository = new Repository(
            name,
            asOf.AddYears(-2),
            asOf.AddDays(-openPullRequestsCount),
            new RepositorySlug(slug));
        repository.UpdateOpenPullRequestsCount(openPullRequestsCount);
        return repository;
    }

    private static PullRequestDetail CreateOpenDemoPullRequest(
        Repository repository,
        int pullRequestId,
        string title,
        DateTimeOffset openedOn,
        string author,
        DateTimeOffset? firstResponseOn,
        DateTimeOffset? lastActivityOn,
        bool hasCurrentUserDiscussion,
        string description,
        int commentsCount,
        int requestChangesCount,
        bool hasCurrentUserRequestChanges,
        int approvalsCount,
        bool hasCurrentUserApproval) =>
        new(
            repository,
            new PullRequestId(pullRequestId),
            title,
            openedOn,
            new BitbucketId($"demo-user-{NormalizeDemoId(author)}"),
            author,
            firstResponseOn,
            lastActivityOn,
            hasCurrentUserDiscussion,
            description,
            commentsCount,
            requestChangesCount,
            hasCurrentUserRequestChanges,
            approvalsCount,
            hasCurrentUserApproval);

    private static void AddOpenDemoPullRequestVariants(List<PullRequestDetail> pullRequests)
    {
        var seedPullRequests = pullRequests.ToArray();
        for (var batch = 1; batch < DEMO_PULL_REQUEST_BATCHES; batch++)
        {
            for (var index = 0; index < seedPullRequests.Length; index++)
            {
                var source = seedPullRequests[index];
                var offset = CreateDemoVariantOffset(batch, index);
                pullRequests.Add(new PullRequestDetail(
                    source.Repository,
                    new PullRequestId(source.PullRequestId.Value + (batch * DEMO_PULL_REQUEST_ID_OFFSET)),
                    $"{source.Title} ({batch + 1})",
                    source.OpenedOn - offset,
                    source.AuthorId,
                    source.AuthorDisplayName,
                    source.FirstNonAuthorActivityOn - offset,
                    source.LastActivityOn - offset,
                    source.HasCurrentUserDiscussion,
                    source.DescriptionText,
                    source.CommentsCount + (index % 4),
                    source.RequestChangesCount,
                    source.HasCurrentUserRequestChanges,
                    source.ApprovalsCount,
                    source.HasCurrentUserApproval));
            }
        }
    }

    private static void AddMergedDemoPullRequestVariants(List<MergedPullRequest> pullRequests)
    {
        var seedPullRequests = pullRequests.ToArray();
        for (var batch = 1; batch < DEMO_PULL_REQUEST_BATCHES; batch++)
        {
            for (var index = 0; index < seedPullRequests.Length; index++)
            {
                var source = seedPullRequests[index];
                var offset = CreateDemoVariantOffset(batch, index);
                pullRequests.Add(new MergedPullRequest(
                    source.Repository,
                    new PullRequestId(source.PullRequestId.Value + (batch * DEMO_PULL_REQUEST_ID_OFFSET)),
                    $"{source.Title} ({batch + 1})",
                    source.OpenedOn - offset,
                    source.AuthorId,
                    source.AuthorDisplayName,
                    source.FirstNonAuthorActivityOn - offset,
                    source.LastActivityOn - offset,
                    source.HasCurrentUserDiscussion,
                    source.MergedOn - offset,
                    source.DescriptionText,
                    source.CommentsCount + (index % 4),
                    source.RequestChangesCount,
                    source.HasCurrentUserRequestChanges,
                    source.ApprovalsCount,
                    source.HasCurrentUserApproval));
            }
        }
    }

    private static TimeSpan CreateDemoVariantOffset(int batch, int index) =>
        TimeSpan.FromDays((batch * 4) + (index * 0.2));

    private static void UpdateDemoRepositoryOpenPullRequestCounts(
        IEnumerable<Repository> repositories,
        IEnumerable<PullRequestDetail> openPullRequests)
    {
        var openCounts = openPullRequests
            .GroupBy(static pullRequest => pullRequest.Repository)
            .ToDictionary(static group => group.Key, static group => group.Count());

        foreach (var repository in repositories)
        {
            repository.UpdateOpenPullRequestsCount(
                openCounts.GetValueOrDefault(repository));
        }
    }

    private static MergedPullRequest CreateMergedDemoPullRequest(
        Repository repository,
        int pullRequestId,
        string title,
        DateTimeOffset openedOn,
        string author,
        DateTimeOffset? firstResponseOn,
        DateTimeOffset? lastActivityOn,
        bool hasCurrentUserDiscussion,
        DateTimeOffset mergedOn,
        string description,
        int commentsCount,
        int requestChangesCount,
        bool hasCurrentUserRequestChanges,
        int approvalsCount,
        bool hasCurrentUserApproval) =>
        new(
            repository,
            new PullRequestId(pullRequestId),
            title,
            openedOn,
            new BitbucketId($"demo-user-{NormalizeDemoId(author)}"),
            author,
            firstResponseOn,
            lastActivityOn,
            hasCurrentUserDiscussion,
            mergedOn,
            description,
            commentsCount,
            requestChangesCount,
            hasCurrentUserRequestChanges,
            approvalsCount,
            hasCurrentUserApproval);

    private static string NormalizeDemoId(string value) => value.Replace(' ', '-').ToUpperInvariant();

    private const int DEMO_PULL_REQUEST_BATCHES = 4;
    private const int DEMO_PULL_REQUEST_ID_OFFSET = 10000;
    private const string DEMO_DESCRIPTION_LONG = "Synthetic demo pull request with realistic review data for presenting the dashboard without Bitbucket credentials.";

    private readonly TimeProvider _timeProvider;
}
