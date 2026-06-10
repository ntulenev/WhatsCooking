using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class RepoPageTests
{
    [Fact(DisplayName = "Repository page preserves values and next link")]
    [Trait("Category", "Unit")]
    public void RepoPageWhenValuesAreValidMapsValues()
    {
        Repository[] repositories = [new("One"), new("Two")];
        var next = new Uri("https://api.bitbucket.org/next");

        var page = new RepoPage(repositories, next);

        page.Values.Should().BeSameAs(repositories);
        page.Next.Should().Be(next);
    }

    [Fact(DisplayName = "Repository page rejects null collection and entries")]
    [Trait("Category", "Unit")]
    public void RepoPageWhenValuesAreInvalidThrowsArgumentException()
    {
        Action nullCollection = () => _ = new RepoPage(null!, null);
        Action nullEntry = () => _ = new RepoPage([null!], null);

        nullCollection.Should().Throw<ArgumentNullException>();
        nullEntry.Should().Throw<ArgumentException>();
    }
}
