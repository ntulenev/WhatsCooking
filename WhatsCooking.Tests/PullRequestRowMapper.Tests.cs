using BBRepoList.Configuration;
using BBRepoList.Models;

using FluentAssertions;

using Microsoft.Extensions.Options;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Tests;

public sealed class PullRequestRowMapperTests
{
    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new PullRequestRowMapper(options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor uses demo workspace when configured workspace is empty")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenDemoWorkspaceIsEmptyUsesDemoWorkspace()
    {
        // Arrange
        var mapper = new PullRequestRowMapper(Options.Create(new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/"),
            DemoMode = true,
            Workspace = string.Empty
        }));
        var asOf = new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
        var detail = new PullRequestDetail(
            new Repository("Demo", slug: new RepositorySlug("demo")),
            new PullRequestId(1),
            "Demo PR",
            asOf,
            null,
            null,
            null,
            null,
            false);

        // Act
        var row = mapper.MapOpen(1, detail, asOf);

        // Assert
        row.RepositoryUrl.Should().Be("https://bitbucket.org/demo-workspace/demo");
    }

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

    [Fact(DisplayName = "MapMerged creates merged pull request row")]
    [Trait("Category", "Unit")]
    public void MapMergedWhenPullRequestIsValidCreatesMergedRow()
    {
        // Arrange
        var mapper = new PullRequestRowMapper(Options.Create(new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/"),
            Workspace = "platform"
        }));
        var asOf = new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
        var pullRequest = new MergedPullRequest(
            new Repository("Payments", slug: new RepositorySlug("payments")),
            new PullRequestId(42),
            "Merged title",
            asOf.AddDays(-2),
            null,
            "Nikita",
            null,
            null,
            false,
            asOf.AddDays(-1));

        // Act
        var row = mapper.MapMerged(4, pullRequest, asOf);

        // Assert
        row.Number.Should().Be(4);
        row.Title.Should().Be("Merged title");
        row.ActivityAgeOrMerged.Should().Be("1d 0h 0m");
    }
}
