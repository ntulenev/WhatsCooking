using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

using BBRepoList.Configuration;

using Microsoft.Extensions.Options;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Services;

/// <summary>
/// Creates AI review prompts from the repository template.
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

        return template
            .Replace("{{BITBUCKET_EMAIL}}", options.AuthEmail, StringComparison.Ordinal)
            .Replace("{{BITBUCKET_API_TOKEN}}", options.AuthApiToken, StringComparison.Ordinal)
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

    [GeneratedRegex(@"\b[A-Z][A-Z0-9]+-\d+\b", RegexOptions.CultureInvariant)]
    private static partial Regex JiraIssueRegex();

    private const string PROMPT_FILE_NAME = "AI_REVIEW_PROMPT.md";

    private readonly IClipboardService _clipboardService;
    private readonly BitbucketOptions _options;
}
