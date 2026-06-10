using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class RepositorySlugTests
{
    [Fact(DisplayName = "Repository slug trims valid value")]
    [Trait("Category", "Unit")]
    public void RepositorySlugWhenValueIsValidTrimsValue()
    {
        var slug = new RepositorySlug(" repository ");

        slug.Value.Should().Be("repository");
        slug.ToString().Should().Be("repository");
    }

    [Fact(DisplayName = "Repository slug rejects empty value")]
    [Trait("Category", "Unit")]
    public void RepositorySlugWhenValueIsEmptyThrowsArgumentException()
    {
        Action act = () => _ = new RepositorySlug(" ");

        act.Should().Throw<ArgumentException>();
    }
}
