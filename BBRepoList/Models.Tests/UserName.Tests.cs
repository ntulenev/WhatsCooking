using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class UserNameTests
{
    [Fact(DisplayName = "User name uses fallback only for null")]
    [Trait("Category", "Unit")]
    public void UserNameWhenCreatedPreservesValueOrUsesFallback()
    {
        var missing = new UserName(null);
        var empty = new UserName(string.Empty);

        missing.Value.Should().Be("<N/A>");
        missing.ToString().Should().Be("<N/A>");
        empty.Value.Should().BeEmpty();
    }
}
