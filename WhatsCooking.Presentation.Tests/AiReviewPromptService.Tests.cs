using BBRepoList.Configuration;
using BBRepoList.Models;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

using WhatsCooking.Services;
using WhatsCooking.ViewModels;

namespace WhatsCooking.Presentation.Tests;

public sealed class AiReviewPromptServiceTests
{
    [Fact(DisplayName = "Constructor throws when clipboard service is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenClipboardServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        IClipboardService clipboardService = null!;

        // Act
        Action act = () => _ = new AiReviewPromptService(clipboardService, CreateOptions());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new AiReviewPromptService(Mock.Of<IClipboardService>(), options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Build prompt replaces PR, credential and Jira placeholders")]
    [Trait("Category", "Unit")]
    public void BuildPromptWhenValuesAreAvailablePopulatesTemplate()
    {
        // Arrange
        const string template =
            "{{BITBUCKET_EMAIL}}|{{BITBUCKET_API_TOKEN}}|{{BITBUCKET_API_BASE_URL}}|" +
            "{{BITBUCKET_WORKSPACE}}|{{PULL_REQUEST_URL}}|" +
            "{{REPOSITORY_NAME}}|{{PULL_REQUEST_ID}}|{{PULL_REQUEST_TITLE}}|" +
            "{{PULL_REQUEST_AUTHOR}}|{{PULL_REQUEST_OPENED_ON}}|" +
            "{{PULL_REQUEST_DESCRIPTION}}|{{JIRA_ISSUE_KEY}}";
        var openedOn = new DateTimeOffset(2026, 6, 12, 10, 30, 0, TimeSpan.Zero);
        var row = CreateRow(
            "ADF-19223 Add retries",
            "Implements the requested retry policy.",
            openedOn);
        var options = CreateOptions().Value;

        // Act
        var result = AiReviewPromptService.BuildPrompt(template, row, options);

        // Assert
        result.Should().Be(
            "user@example.com|secret-token|https://api.bitbucket.org/2.0/|platform|" +
            "https://bitbucket.org/platform/payments/pull-requests/42|" +
            "Payments|42|ADF-19223 Add retries|Nikita|2026-06-12T10:30:00.0000000+00:00|" +
            "Implements the requested retry policy.|ADF-19223");
    }

    [Fact(DisplayName = "Build prompt uses fallbacks when description and Jira key are absent")]
    [Trait("Category", "Unit")]
    public void BuildPromptWhenOptionalValuesAreAbsentUsesFallbacks()
    {
        // Arrange
        var row = CreateRow("Improve retry policy", descriptionText: null);

        // Act
        var result = AiReviewPromptService.BuildPrompt(
            "{{PULL_REQUEST_DESCRIPTION}}|{{JIRA_ISSUE_KEY}}",
            row,
            CreateOptions().Value);

        // Assert
        result.Should().Be("(not available)|(not detected)");
    }

    [Theory(DisplayName = "Build prompt rejects an invalid template")]
    [Trait("Category", "Unit")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildPromptWhenTemplateIsInvalidThrowsArgumentException(string? template)
    {
        // Act
        Action act = () => AiReviewPromptService.BuildPrompt(
            template!,
            CreateRow("Pull request"),
            CreateOptions().Value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Copy prompt reads the deployed template and sends populated text to clipboard")]
    [Trait("Category", "Integration")]
    public void CopyPromptWhenTemplateExistsCopiesPopulatedPrompt()
    {
        // Arrange
        var templatePath = Path.Combine(AppContext.BaseDirectory, "AI_REVIEW_PROMPT.md");
        var existingTemplate = File.Exists(templatePath) ? File.ReadAllBytes(templatePath) : null;
        File.WriteAllText(templatePath, "{{PULL_REQUEST_TITLE}}|{{JIRA_ISSUE_KEY}}");
        var clipboard = new Mock<IClipboardService>(MockBehavior.Strict);
        clipboard.Setup(instance => instance.SetText("ABC-42 Improve retries|ABC-42"));
        var service = new AiReviewPromptService(clipboard.Object, CreateOptions());

        try
        {
            // Act
            service.CopyPrompt(CreateRow("ABC-42 Improve retries"));
        }
        finally
        {
            if (existingTemplate is null)
            {
                File.Delete(templatePath);
            }
            else
            {
                File.WriteAllBytes(templatePath, existingTemplate);
            }
        }

        // Assert
        clipboard.VerifyAll();
    }

    [Fact(DisplayName = "Copy prompt throws when pull request is null")]
    [Trait("Category", "Unit")]
    public void CopyPromptWhenPullRequestIsNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new AiReviewPromptService(Mock.Of<IClipboardService>(), CreateOptions());

        // Act
        Action act = () => service.CopyPrompt(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private static PullRequestRow CreateRow(
        string title,
        string? descriptionText = "Description",
        DateTimeOffset? openedOn = null)
    {
        var opened = openedOn ?? new DateTimeOffset(2026, 6, 12, 10, 30, 0, TimeSpan.Zero);
        var detail = new PullRequestDetail(
            new Repository("Payments", slug: new RepositorySlug("payments")),
            new PullRequestId(42),
            title,
            opened,
            authorId: null,
            authorDisplayName: "Nikita",
            firstNonAuthorActivityOn: null,
            lastActivityOn: null,
            hasCurrentUserDiscussion: false,
            descriptionText: descriptionText);
        return new PullRequestRow(
            1,
            detail,
            opened.AddHours(1),
            new PullRequestPresentationOptions(
                new BitbucketWorkspace("platform"),
                10,
                TimeSpan.FromHours(4)));
    }

    private static IOptions<BitbucketOptions> CreateOptions() =>
        Options.Create(new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/"),
            Workspace = "platform",
            AuthEmail = "user@example.com",
            AuthApiToken = "secret-token"
        });
}
