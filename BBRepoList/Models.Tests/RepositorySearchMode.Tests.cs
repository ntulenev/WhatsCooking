using FluentAssertions;

namespace BBRepoList.Models.Tests;

public sealed class RepositorySearchModeTests
{
    [Fact(DisplayName = "Repository search mode exposes supported values")]
    [Trait("Category", "Unit")]
    public void RepositorySearchModeWhenEnumeratedContainsSupportedValues()
    {
        var values = Enum.GetValues<RepositorySearchMode>();

        values.Should().Equal(RepositorySearchMode.Contains, RepositorySearchMode.StartWith);
    }
}
