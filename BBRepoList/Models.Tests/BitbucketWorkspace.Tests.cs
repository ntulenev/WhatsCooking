using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class BitbucketWorkspaceTests
{
    [Fact(DisplayName = "Workspace trims valid value")]
    [Trait("Category", "Unit")]
    public void BitbucketWorkspaceWhenValueIsValidTrimsValue()
    {
        var workspace = new BitbucketWorkspace(" workspace ");

        workspace.Value.Should().Be("workspace");
        workspace.ToString().Should().Be("workspace");
    }

    [Fact(DisplayName = "Workspace rejects empty value")]
    [Trait("Category", "Unit")]
    public void BitbucketWorkspaceWhenValueIsEmptyThrowsArgumentException()
    {
        Action act = () => _ = new BitbucketWorkspace(" ");

        act.Should().Throw<ArgumentException>();
    }
}
