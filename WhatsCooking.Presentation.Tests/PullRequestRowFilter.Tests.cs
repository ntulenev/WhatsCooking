using BBRepoList.Models;

using FluentAssertions;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Presentation.Tests;

public sealed class PullRequestRowFilterTests
{
    [Fact(DisplayName = "Matches throws when row is null")]
    [Trait("Category", "Unit")]
    public void MatchesWhenRowIsNullThrowsArgumentNullException()
    {
        // Arrange
        PullRequestRow row = null!;
        var filters = new PullRequestFilterState(() => { });

        // Act
        Action act = () => PullRequestRowFilter.Matches(row, string.Empty, filters);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Matches throws when filters are null")]
    [Trait("Category", "Unit")]
    public void MatchesWhenFiltersAreNullThrowsArgumentNullException()
    {
        // Arrange
        var row = CreateRow();
        PullRequestFilterState filters = null!;

        // Act
        Action act = () => PullRequestRowFilter.Matches(row, string.Empty, filters);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Matches accepts empty filters and trims case-insensitive global search")]
    [Trait("Category", "Unit")]
    public void MatchesWhenFiltersAreEmptyUsesTrimmedCaseInsensitiveGlobalSearch()
    {
        // Arrange
        var row = CreateRow();
        var filters = new PullRequestFilterState(() => { });

        // Act
        var emptyMatches = PullRequestRowFilter.Matches(row, "  ", filters);
        var globalMatches = PullRequestRowFilter.Matches(row, "  ADD RETRIES  ", filters);
        var missingMatches = PullRequestRowFilter.Matches(row, "missing", filters);

        // Assert
        emptyMatches.Should().BeTrue();
        globalMatches.Should().BeTrue();
        missingMatches.Should().BeFalse();
    }

    [Fact(DisplayName = "Matches applies every column filter")]
    [Trait("Category", "Unit")]
    public void MatchesWhenColumnFiltersAreSetRequiresEveryFilterToMatch()
    {
        // Arrange
        var row = CreateRow();
        var filters = new PullRequestFilterState(() => { });
        (Action<PullRequestFilterState, string> SetFilter, string Match, string Name)[] cases =
        [
            (static (state, value) => state.Number = value, " 7 ", nameof(filters.Number)),
            (static (state, value) => state.Repository = value, "payments", nameof(filters.Repository)),
            (static (state, value) => state.PullRequest = value, "ADD RETRIES", nameof(filters.PullRequest)),
            (static (state, value) => state.Author = value, "nikita", nameof(filters.Author)),
            (static (state, value) => state.DescriptionLength = value, "11", nameof(filters.DescriptionLength)),
            (static (state, value) => state.OpenFor = value, "1d 2h", nameof(filters.OpenFor)),
            (static (state, value) => state.TimeToFirstResponse = value, "1h 3m", nameof(filters.TimeToFirstResponse)),
            (static (state, value) => state.Activity = value, "45m", nameof(filters.Activity)),
            (static (state, value) => state.Comments = value, "5", nameof(filters.Comments)),
            (static (state, value) => state.RequestChanges = value, "rc (1)", nameof(filters.RequestChanges)),
            (static (state, value) => state.Approvals = value, "ap (2)", nameof(filters.Approvals)),
            (static (state, value) => state.CurrentUserActivity = value, "request changes", nameof(filters.CurrentUserActivity))
        ];

        // Act & Assert
        foreach (var (SetFilter, Match, Name) in cases)
        {
            SetFilter(filters, Match);
            PullRequestRowFilter.Matches(row, string.Empty, filters)
                .Should().BeTrue($"the {Name} filter matches");

            SetFilter(filters, "missing");
            PullRequestRowFilter.Matches(row, string.Empty, filters)
                .Should().BeFalse($"the {Name} filter does not match");

            SetFilter(filters, string.Empty);
        }
    }

    [Fact(DisplayName = "Matches hides reviewed rows when requested")]
    [Trait("Category", "Unit")]
    public void MatchesWhenHideReviewedIsEnabledExcludesReviewedRows()
    {
        // Arrange
        var row = CreateRow();
        var filters = new PullRequestFilterState(() => { });

        // Act
        row.IsReviewed = true;
        var visibleByDefault = PullRequestRowFilter.Matches(row, string.Empty, filters);
        filters.HideReviewed = true;
        var hiddenWhenFiltered = PullRequestRowFilter.Matches(row, string.Empty, filters);
        row.IsReviewed = false;
        var unreviewedMatches = PullRequestRowFilter.Matches(row, string.Empty, filters);

        // Assert
        visibleByDefault.Should().BeTrue();
        hiddenWhenFiltered.Should().BeFalse();
        unreviewedMatches.Should().BeTrue();
    }

    private static PullRequestRow CreateRow()
    {
        var asOf = new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
        var detail = new PullRequestDetail(
            new Repository("Payments API", slug: new RepositorySlug("payments-api")),
            new PullRequestId(42),
            "Add retries",
            asOf.AddDays(-1).AddHours(-2).AddMinutes(-3),
            null,
            "Nikita",
            asOf.AddHours(-25),
            asOf.AddMinutes(-45),
            true,
            "description",
            commentsCount: 5,
            requestChangesCount: 1,
            hasCurrentUserRequestChanges: true,
            approvalsCount: 2,
            hasCurrentUserApproval: true);
        var options = new PullRequestPresentationOptions(
            new BitbucketWorkspace("platform"),
            10,
            TimeSpan.FromHours(4));

        return new PullRequestRow(7, detail, asOf, options);
    }
}
