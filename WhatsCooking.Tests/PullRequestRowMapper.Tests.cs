using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

using WhatsCooking.ViewModels;

using Xunit;

namespace WhatsCooking.Tests;

public sealed class PullRequestRowMapperTests
{
    [Fact]
    public void MapOpenUsesOnlyPresentationConfiguration()
    {
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

        var row = mapper.MapOpen(1, pullRequest, asOf);

        Assert.True(row.IsDescriptionShort);
        Assert.True(row.IsTtfrAlert);
        Assert.Equal("https://bitbucket.org/platform/payments/pull-requests/42", row.PullRequestUrl.ToString());
    }

    [Fact]
    public void FilterRequiresGlobalAndColumnMatches()
    {
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

        Assert.True(PullRequestRowFilter.Matches(row, "settlement", filters));

        filters.Author = "someone else";
        Assert.False(PullRequestRowFilter.Matches(row, "settlement", filters));
    }
}
