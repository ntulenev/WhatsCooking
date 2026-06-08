using BBRepoList.Configuration;
using BBRepoList.Models;

using FluentAssertions;

using Microsoft.Extensions.Options;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Tests;

public sealed class PullRequestRowMapperTests
{
    [Fact(DisplayName = "MapOpen uses presentation configuration")]
    [Trait("Category", "Unit")]
    public void MapOpenUsesOnlyPresentationConfiguration()
    {
        // Arrange
        var mapper = new PullRequestRowMapper(Options.Create(new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0"),
            Workspace = "platform",
            PullRequestDetails = new PullRequestDetailsOptions
            {
                MinimalDescriptionTextLength = 10,
                TtfrThresholdHours = 4
            }
        }));
        var asOf = new DateTimeOffset(2026, 6, 7, 10, 0, 0, TimeSpan.Zero);
        var pullRequest = new PullRequestDetail(
            new Repository("Payments", slug: new RepositorySlug("payments")),
            new PullRequestId(42),
            "Add settlement retry",
            asOf.AddHours(-5),
            null,
            "Nikita",
            null,
            asOf.AddHours(-1),
            false,
            "short");

        // Act
        var row = mapper.MapOpen(1, pullRequest, asOf);

        // Assert
        row.IsDescriptionShort.Should().BeTrue();
        row.IsTtfrAlert.Should().BeTrue();
        row.PullRequestUrl.Should().Be("https://bitbucket.org/platform/payments/pull-requests/42");
    }

    [Fact(DisplayName = "Pull request row filter requires global and column matches")]
    [Trait("Category", "Unit")]
    public void FilterRequiresGlobalAndColumnMatches()
    {
        // Arrange
        var options = new PullRequestPresentationOptions(
            new BitbucketWorkspace("platform"),
            1,
            TimeSpan.FromHours(4));
        var asOf = new DateTimeOffset(2026, 6, 7, 10, 0, 0, TimeSpan.Zero);
        var row = new PullRequestRow(
            1,
            new PullRequestDetail(
                new Repository("Payments", slug: new RepositorySlug("payments")),
                new PullRequestId(42),
                "Add settlement retry",
                asOf.AddHours(-2),
                null,
                "Nikita",
                asOf.AddHours(-1),
                asOf.AddHours(-1),
                true),
            asOf,
            options);
        var filters = new PullRequestFilterState(static () => { })
        {
            Author = "nik",
            Repository = "pay"
        };

        // Act
        var matchingResult = PullRequestRowFilter.Matches(row, "settlement", filters);
        filters.Author = "someone else";
        var nonMatchingResult = PullRequestRowFilter.Matches(row, "settlement", filters);

        // Assert
        matchingResult.Should().BeTrue();
        nonMatchingResult.Should().BeFalse();
    }
}
