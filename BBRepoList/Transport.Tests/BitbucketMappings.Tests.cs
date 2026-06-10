using BBRepoList.Models;

using FluentAssertions;

namespace BBRepoList.Transport.Tests;

public sealed class BitbucketMappingsTests
{
    [Fact(DisplayName = "Repository mapping maps all values")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenRepositoryDtoIsValidMapsValues()
    {
        var createdOn = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedOn = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var dto = new RepositoryDto(" Repository ", createdOn, updatedOn, " repository ");

        var result = dto.ToDomain();

        result.Name.Should().Be("Repository");
        result.CreatedOn.Should().Be(createdOn);
        result.LastUpdatedOn.Should().Be(updatedOn);
        result.Slug.Should().Be(new RepositorySlug("repository"));
    }

    [Fact(DisplayName = "Repository mapping allows missing slug")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenRepositorySlugIsMissingMapsNullSlug()
    {
        var result = new RepositoryDto("Repository", Slug: " ").ToDomain();

        result.Slug.Should().BeNull();
    }

    [Fact(DisplayName = "Repository mapping rejects null DTO")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenRepositoryDtoIsNullThrowsArgumentNullException()
    {
        RepositoryDto dto = null!;

        Action act = () => dto.ToDomain();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Repository mapping rejects missing name")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenRepositoryNameIsMissingThrowsArgumentException()
    {
        Action act = () => new RepositoryDto(null).ToDomain();

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Repository page mapping maps values and next link")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenRepositoryPageIsValidMapsValues()
    {
        var next = new Uri("https://api.bitbucket.org/next");
        var dto = new RepoPageDto([new RepositoryDto("One"), new RepositoryDto("Two")], next);

        var result = dto.ToDomain();

        result.Values.Select(static repository => repository.Name).Should().Equal("One", "Two");
        result.Next.Should().Be(next);
    }

    [Fact(DisplayName = "Repository page mapping allows missing values")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenRepositoryPageValuesAreMissingMapsEmptyPage()
    {
        var result = new RepoPageDto(null, null).ToDomain();

        result.Values.Should().BeEmpty();
        result.Next.Should().BeNull();
    }

    [Fact(DisplayName = "Repository page mapping rejects null item")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenRepositoryPageContainsNullThrowsArgumentException()
    {
        var dto = new RepoPageDto([null!], null);

        Action act = () => dto.ToDomain();

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Repository page mapping rejects null DTO")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenRepositoryPageDtoIsNullThrowsArgumentNullException()
    {
        RepoPageDto dto = null!;

        Action act = () => dto.ToDomain();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "User mapping maps identifier and display name")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenUserDtoIsValidMapsValues()
    {
        var result = new BitbucketUserDto("{USER}", "User Name").ToDomain();

        result.Uuid.Should().Be(new BitbucketId("user"));
        result.DisplayName.Should().Be(new UserName("User Name"));
    }

    [Fact(DisplayName = "User mapping rejects null DTO")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenUserDtoIsNullThrowsArgumentNullException()
    {
        BitbucketUserDto dto = null!;

        Action act = () => dto.ToDomain();

        act.Should().Throw<ArgumentNullException>();
    }
}
