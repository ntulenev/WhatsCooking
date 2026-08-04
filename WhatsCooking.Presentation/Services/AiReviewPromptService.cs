using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

using BBRepoList.Configuration;

using Microsoft.Extensions.Options;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Services;

/// <summary>
/// Creates AI pull request prompts from repository templates.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Service is created by dependency injection.")]
internal sealed partial class AiReviewPromptService : IAiReviewPromptService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiReviewPromptService"/> class.
    /// </summary>
    /// <param name="clipboardService">Clipboard service.</param>
    /// <param name="options">Bitbucket configuration.</param>
    public AiReviewPromptService(
        IClipboardService clipboardService,
        IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(clipboardService);
        ArgumentNullException.ThrowIfNull(options);

        _clipboardService = clipboardService;
        _options = options.Value;
    }

    /// <inheritdoc />
    public void CopyPrompt(PullRequestRow pullRequest)
    {
        ArgumentNullException.ThrowIfNull(pullRequest);

        var templatePath = Path.Combine(AppContext.BaseDirectory, PROMPT_FILE_NAME);
        var template = File.ReadAllText(templatePath);
        _clipboardService.SetText(BuildPrompt(template, pullRequest, _options));
    }

    /// <inheritdoc />
    public void CopyTeamOverviewPrompt(
        IReadOnlyCollection<PullRequestRow> pullRequests,
        bool isMerged)
    {
        ArgumentNullException.ThrowIfNull(pullRequests);

        var templatePath = Path.Combine(AppContext.BaseDirectory, TEAM_OVERVIEW_PROMPT_FILE_NAME);
        var template = File.ReadAllText(templatePath);
        _clipboardService.SetText(BuildTeamOverviewPrompt(template, pullRequests, isMerged, _options));
    }

    internal static string BuildPrompt(
        string template,
        PullRequestRow pullRequest,
        BitbucketOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        ArgumentNullException.ThrowIfNull(pullRequest);
        ArgumentNullException.ThrowIfNull(options);

        var jiraIssueKey = JiraIssueRegex().Match(
            string.Join(' ', pullRequest.Title, pullRequest.DescriptionText)).Value;

        return ReplaceAccessPlaceholders(template, options)
            .Replace("{{PULL_REQUEST_URL}}", pullRequest.PullRequestUrl.ToString(), StringComparison.Ordinal)
            .Replace("{{REPOSITORY_NAME}}", pullRequest.RepositoryName, StringComparison.Ordinal)
            .Replace(
                "{{PULL_REQUEST_ID}}",
                pullRequest.PullRequestId.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal)
            .Replace("{{PULL_REQUEST_TITLE}}", pullRequest.Title, StringComparison.Ordinal)
            .Replace("{{PULL_REQUEST_AUTHOR}}", pullRequest.Author, StringComparison.Ordinal)
            .Replace(
                "{{PULL_REQUEST_OPENED_ON}}",
                pullRequest.OpenedOn.ToString("O", CultureInfo.InvariantCulture),
                StringComparison.Ordinal)
            .Replace(
                "{{PULL_REQUEST_DESCRIPTION}}",
                pullRequest.DescriptionText ?? "(not available)",
                StringComparison.Ordinal)
            .Replace(
                "{{JIRA_ISSUE_KEY}}",
                string.IsNullOrWhiteSpace(jiraIssueKey) ? "(not detected)" : jiraIssueKey,
                StringComparison.Ordinal);
    }

    internal static string BuildTeamOverviewPrompt(
        string template,
        IReadOnlyCollection<PullRequestRow> pullRequests,
        bool isMerged,
        BitbucketOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        ArgumentNullException.ThrowIfNull(pullRequests);
        ArgumentNullException.ThrowIfNull(options);
        if (pullRequests.Count == 0)
        {
            throw new ArgumentException("At least one pull request is required.", nameof(pullRequests));
        }

        var pullRequestList = string.Join(
            Environment.NewLine,
            pullRequests.Select(static pullRequest => $"- {pullRequest.PullRequestUrl}"));

        return ReplaceAccessPlaceholders(template, options)
            .Replace(
                "{{PULL_REQUEST_SCOPE}}",
                isMerged ? "merged" : "open",
                StringComparison.Ordinal)
            .Replace(
                "{{ANALYSIS_CONTEXT}}",
                isMerged
                    ? "Treat them as completed work and assess them retrospectively, not as work to intervene in."
                    : "Treat them as active work.",
                StringComparison.Ordinal)
            .Replace("{{PULL_REQUEST_LIST}}", pullRequestList, StringComparison.Ordinal)
            .Replace(
                "{{TEAM_FOCUS_INSTRUCTION}}",
                isMerged ? "summarize what the team completed" : "summarize what the team is working on",
                StringComparison.Ordinal)
            .Replace(
                "{{ATTENTION_INSTRUCTION}}",
                isMerged
                    ? "identify completed PRs worth retrospective attention because of impact, risk, delays, problems, or follow-up needs"
                    : "identify PRs that need attention because of impact, risk, problems, conflicts, stalled work, or review gaps",
                StringComparison.Ordinal);
    }

    private static string ReplaceAccessPlaceholders(string template, BitbucketOptions options) =>
        template
            .Replace("{{BITBUCKET_EMAIL}}", options.AuthEmail, StringComparison.Ordinal)
            .Replace("{{BITBUCKET_API_TOKEN}}", options.AuthApiToken, StringComparison.Ordinal)
            .Replace(
                "{{BITBUCKET_API_BASE_URL}}",
                options.BaseUrl.ToString(),
                StringComparison.Ordinal)
            .Replace("{{BITBUCKET_WORKSPACE}}", options.Workspace, StringComparison.Ordinal);

    [GeneratedRegex(@"\b[A-Z][A-Z0-9]+-\d+\b", RegexOptions.CultureInvariant)]
    private static partial Regex JiraIssueRegex();

    private const string PROMPT_FILE_NAME = "AI_REVIEW_PROMPT.md";
    private const string TEAM_OVERVIEW_PROMPT_FILE_NAME = "AI_TEAM_OVERVIEW_PROMPT.md";

    private readonly IClipboardService _clipboardService;
    private readonly BitbucketOptions _options;
}
