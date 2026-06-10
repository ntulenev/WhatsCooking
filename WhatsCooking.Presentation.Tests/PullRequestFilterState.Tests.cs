using FluentAssertions;

using WhatsCooking.ViewModels;

namespace WhatsCooking.Presentation.Tests;

public sealed class PullRequestFilterStateTests
{
    [Fact(DisplayName = "Constructor throws when change callback is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenChangeCallbackIsNullThrowsArgumentNullException()
    {
        // Arrange
        Action onChanged = null!;

        // Act
        Action act = () => _ = new PullRequestFilterState(onChanged);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Changing filter publishes property change and callback")]
    [Trait("Category", "Unit")]
    public void FilterPropertyWhenValueChangesPublishesNotifications()
    {
        // Arrange
        var callbackCalls = 0;
        var propertyNames = new List<string?>();
        var filters = new PullRequestFilterState(() => callbackCalls++);
        filters.PropertyChanged += (_, args) => propertyNames.Add(args.PropertyName);

        // Act
        filters.Author = "Nikita";
        filters.Author = "Nikita";

        // Assert
        filters.Author.Should().Be("Nikita");
        callbackCalls.Should().Be(1);
        propertyNames.Should().Equal(nameof(PullRequestFilterState.Author));
    }

    [Fact(DisplayName = "Reset clears every filter")]
    [Trait("Category", "Unit")]
    public void ResetWhenFiltersHaveValuesClearsEveryFilter()
    {
        // Arrange
        var filters = new PullRequestFilterState(() => { })
        {
            Number = "1",
            Repository = "repo",
            PullRequest = "title",
            Author = "author",
            DescriptionLength = "10",
            OpenFor = "1h",
            TimeToFirstResponse = "2h",
            Activity = "3h",
            Comments = "4",
            RequestChanges = "RC",
            Approvals = "AP",
            CurrentUserActivity = "Comment",
            HideReviewed = true
        };

        // Act
        filters.Reset();

        // Assert
        filters.Should().BeEquivalentTo(new
        {
            Number = string.Empty,
            Repository = string.Empty,
            PullRequest = string.Empty,
            Author = string.Empty,
            DescriptionLength = string.Empty,
            OpenFor = string.Empty,
            TimeToFirstResponse = string.Empty,
            Activity = string.Empty,
            Comments = string.Empty,
            RequestChanges = string.Empty,
            Approvals = string.Empty,
            CurrentUserActivity = string.Empty,
            HideReviewed = false
        });
    }
}
