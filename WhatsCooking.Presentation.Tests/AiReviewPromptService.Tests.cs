using BBRepoList.Configuration;
using BBRepoList.Models;

using FluentAssertions;

using WhatsCooking.Services;
using WhatsCooking.ViewModels;

namespace WhatsCooking.Presentation.Tests;

public sealed class AiReviewPromptServiceTests
{
    [Fact(DisplayName = "Build prompt replaces PR, credential and Jira placeholders")]
    [Trait("Category", "Unit")]
    public void BuildPromptWhenValuesAreAvailablePopulatesTemplate()
    {
        // Arrange
        const string template =
            "{{BITBUCKET_EMAIL}}|{{BITBUCKET_API_TOKEN}}|{{PULL_REQUEST_URL}}|" +
            "{{REPOSITORY_NAME}}|{{PULL_REQUEST_ID}}|{{PULL_REQUEST_TITLE}}|" +
            "{{PULL_REQUEST_AUTHOR}}|{{PULL_REQUEST_OPENED_ON}}|" +
            "{{PULL_REQUEST_DESCRIPTION}}|{{JIRA_ISSUE_KEY}}";
        var openedOn = new DateTimeOffset(2026, 6, 12, 10, 30, 0, TimeSpan.Zero);
        var detail = new PullRequestDetail(
            new Repository("Payments", slug: new RepositorySlug("payments")),
            new PullRequestId(42),
            "ADF-19223 Add retries",
            openedOn,
            authorId: null,
            authorDisplayName: "Nikita",
            firstNonAuthorActivityOn: null,
            lastActivityOn: null,
            hasCurrentUserDiscussion: false,
            descriptionText: "Implements the requested retry policy.");
        var row = new PullRequestRow(
            1,
            detail,
            openedOn.AddHours(1),
            new PullRequestPresentationOptions(
                new BitbucketWorkspace("platform"),
                10,
                TimeSpan.FromHours(4)));
        var options = new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/"),
            AuthEmail = "user@example.com",
            AuthApiToken = "secret-token"
        };

        // Act
        var result = AiReviewPromptService.BuildPrompt(template, row, options);

        // Assert
        result.Should().Be(
            "user@example.com|secret-token|https://bitbucket.org/platform/payments/pull-requests/42|" +
            "Payments|42|ADF-19223 Add retries|Nikita|2026-06-12T10:30:00.0000000+00:00|" +
            "Implements the requested retry policy.|ADF-19223");
    }
}
