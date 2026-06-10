using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class BitbucketUserTests
{
    [Fact(DisplayName = "Bitbucket user preserves values")]
    [Trait("Category", "Unit")]
    public void BitbucketUserWhenCreatedPreservesValues()
    {
        var user = new BitbucketUser(new BitbucketId("user"), new UserName("User"));

        user.Uuid.Should().Be(new BitbucketId("{USER}"));
        user.DisplayName.Should().Be(new UserName("User"));
    }
}
