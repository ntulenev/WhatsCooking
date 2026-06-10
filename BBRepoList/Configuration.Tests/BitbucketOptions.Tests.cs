using System.ComponentModel.DataAnnotations;

using FluentAssertions;

namespace BBRepoList.Configuration.Tests;

public sealed class BitbucketOptionsTests
{
    [Fact(DisplayName = "Validate accepts valid production configuration")]
    [Trait("Category", "Unit")]
    public void ValidateWhenProductionConfigurationIsValidReturnsNoErrors()
    {
        // Arrange
        var options = CreateValidOptions();

        // Act
        var results = Validate(options);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact(DisplayName = "Validate allows missing credentials in demo mode")]
    [Trait("Category", "Unit")]
    public void ValidateWhenDemoModeIsEnabledAllowsMissingCredentials()
    {
        // Arrange
        var options = CreateOptions(
            demoMode: true,
            workspace: string.Empty,
            authEmail: string.Empty,
            authApiToken: string.Empty);

        // Act
        var results = Validate(options);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact(DisplayName = "Validate requires credentials outside demo mode")]
    [Trait("Category", "Unit")]
    public void ValidateWhenDemoModeIsDisabledRequiresCredentials()
    {
        // Arrange
        var options = CreateOptions(
            workspace: " ",
            authEmail: string.Empty,
            authApiToken: "\t");

        // Act
        var results = Validate(options);

        // Assert
        results.Should().HaveCount(3);
        results.SelectMany(static result => result.MemberNames)
            .Should()
            .BeEquivalentTo(
                nameof(BitbucketOptions.Workspace),
                nameof(BitbucketOptions.AuthEmail),
                nameof(BitbucketOptions.AuthApiToken));
    }

    [Fact(DisplayName = "Validate applies top-level data annotation rules")]
    [Trait("Category", "Unit")]
    public void ValidateWhenTopLevelValuesAreInvalidReturnsValidationErrors()
    {
        // Arrange
        var options = CreateOptions(
            includeBaseUrl: false,
            pageLen: 0,
            retryCount: 11);

        // Act
        var results = Validate(options);

        // Assert
        results.SelectMany(static result => result.MemberNames)
            .Should()
            .BeEquivalentTo(
                nameof(BitbucketOptions.BaseUrl),
                nameof(BitbucketOptions.PageLen),
                nameof(BitbucketOptions.RetryCount));
    }

    [Fact(DisplayName = "Validate applies nested pull request detail rules")]
    [Trait("Category", "Unit")]
    public void ValidateWhenPullRequestDetailsAreInvalidReturnsNestedValidationErrors()
    {
        // Arrange
        var options = CreateOptions(
            pullRequestDetails: new PullRequestDetailsOptions
            {
                TtfrThresholdHours = 0,
                MinimalDescriptionTextLength = 10001,
                LoadThreshold = 65
            });

        // Act
        var results = Validate(options);

        // Assert
        results.SelectMany(static result => result.MemberNames)
            .Should()
            .BeEquivalentTo(
                nameof(PullRequestDetailsOptions.TtfrThresholdHours),
                nameof(PullRequestDetailsOptions.MinimalDescriptionTextLength),
                nameof(PullRequestDetailsOptions.LoadThreshold));
    }

    [Fact(DisplayName = "Validate applies nested merged pull request rules")]
    [Trait("Category", "Unit")]
    public void ValidateWhenMergedPullRequestsAreInvalidReturnsNestedValidationError()
    {
        // Arrange
        var options = CreateOptions(
            mergedPullRequests: new MergedPullRequestsOptions
            {
                LoadThreshold = 0
            });

        // Act
        var results = Validate(options);

        // Assert
        results.Should().ContainSingle();
        results[0].MemberNames.Should().ContainSingle()
            .Which.Should().Be(nameof(MergedPullRequestsOptions.LoadThreshold));
    }

    [Fact(DisplayName = "Validate throws when validation context is null")]
    [Trait("Category", "Unit")]
    public void ValidateWhenValidationContextIsNullThrowsArgumentNullException()
    {
        // Arrange
        var options = CreateValidOptions();
        ValidationContext validationContext = null!;

        // Act
        Action act = () => _ = options.Validate(validationContext).ToArray();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private static List<ValidationResult> Validate(BitbucketOptions options)
    {
        var results = new List<ValidationResult>();
        _ = Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            results,
            validateAllProperties: true);
        return results;
    }

    private static BitbucketOptions CreateValidOptions() => CreateOptions();

    private static BitbucketOptions CreateOptions(
        bool demoMode = false,
        string workspace = "workspace",
        string authEmail = "user@example.com",
        string authApiToken = "token",
        bool includeBaseUrl = true,
        int pageLen = 50,
        int retryCount = 3,
        PullRequestDetailsOptions? pullRequestDetails = null,
        MergedPullRequestsOptions? mergedPullRequests = null) =>
        new()
        {
            DemoMode = demoMode,
            BaseUrl = includeBaseUrl ? new Uri("https://api.bitbucket.org/2.0/") : null!,
            Workspace = workspace,
            AuthEmail = authEmail,
            AuthApiToken = authApiToken,
            PageLen = pageLen,
            RetryCount = retryCount,
            PullRequestDetails = pullRequestDetails ?? new PullRequestDetailsOptions(),
            MergedPullRequests = mergedPullRequests ?? new MergedPullRequestsOptions()
        };
}
