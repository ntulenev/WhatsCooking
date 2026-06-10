using FluentAssertions;

namespace BBRepoList.Configuration.Tests;

public sealed class ConfigurationDefaultsTests
{
    [Fact(DisplayName = "Bitbucket options initializes nested options")]
    [Trait("Category", "Unit")]
    public void BitbucketOptionsWhenCreatedInitializesNestedOptions()
    {
        // Act
        var options = new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/")
        };

        // Assert
        options.DemoMode.Should().BeFalse();
        options.Workspace.Should().BeEmpty();
        options.AuthEmail.Should().BeEmpty();
        options.AuthApiToken.Should().BeEmpty();
        options.PageLen.Should().Be(0);
        options.RetryCount.Should().Be(0);
        options.PullRequestDetails.Should().NotBeNull();
        options.MergedPullRequests.Should().NotBeNull();
        options.Telemetry.Should().NotBeNull();
    }

    [Fact(DisplayName = "Pull request details options uses report defaults")]
    [Trait("Category", "Unit")]
    public void PullRequestDetailsOptionsWhenCreatedUsesDefaults()
    {
        // Act
        var options = new PullRequestDetailsOptions();

        // Assert
        options.TtfrThresholdHours.Should().Be(4);
        options.MinimalDescriptionTextLength.Should().Be(1);
        options.LoadThreshold.Should().Be(8);
    }

    [Fact(DisplayName = "Merged pull requests options uses load threshold default")]
    [Trait("Category", "Unit")]
    public void MergedPullRequestsOptionsWhenCreatedUsesDefault()
    {
        // Act
        var options = new MergedPullRequestsOptions();

        // Assert
        options.LoadThreshold.Should().Be(8);
    }

    [Fact(DisplayName = "Telemetry options is disabled by default")]
    [Trait("Category", "Unit")]
    public void BitbucketTelemetryOptionsWhenCreatedIsDisabled()
    {
        // Act
        var options = new BitbucketTelemetryOptions();

        // Assert
        options.Enabled.Should().BeFalse();
    }
}
