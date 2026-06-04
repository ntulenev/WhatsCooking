using System.ComponentModel.DataAnnotations;

namespace BBRepoList.Configuration;

/// <summary>
/// Configuration settings for Bitbucket API access.
/// </summary>
public sealed class BitbucketOptions
    : IValidatableObject
{
    /// <summary>
    /// Gets a value indicating whether the UI should load synthetic demo data instead of calling Bitbucket.
    /// </summary>
    public bool DemoMode { get; init; }

    /// <summary>
    /// Base Bitbucket API URL.
    /// </summary>
    [Required]
    public required Uri BaseUrl { get; init; }

    /// <summary>
    /// Bitbucket workspace identifier.
    /// </summary>
    public string Workspace { get; init; } = string.Empty;

    /// <summary>
    /// Authentication email for Bitbucket API.
    /// </summary>
    public string AuthEmail { get; init; } = string.Empty;

    /// <summary>
    /// Authentication API token for Bitbucket API.
    /// </summary>
    public string AuthApiToken { get; init; } = string.Empty;

    /// <summary>
    /// Number of repositories per page.
    /// </summary>
    [Range(1, 100)]
    public int PageLen { get; init; }

    /// <summary>
    /// Number of retries for transient Bitbucket API errors.
    /// </summary>
    [Range(0, 10)]
    public int RetryCount { get; init; }

    /// <summary>
    /// Pull request details report settings.
    /// </summary>
    [Required]
    public PullRequestDetailsOptions PullRequestDetails { get; init; } = new();

    /// <summary>
    /// Recently merged pull request report settings.
    /// </summary>
    [Required]
    public MergedPullRequestsOptions MergedPullRequests { get; init; } = new();

    /// <summary>
    /// Bitbucket API request telemetry settings.
    /// </summary>
    [Required]
    public BitbucketTelemetryOptions Telemetry { get; init; } = new();

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        if (!DemoMode)
        {
            foreach (var result in ValidateRequiredBitbucketCredentials())
            {
                yield return result;
            }
        }

        foreach (var result in ValidateNestedOptions(PullRequestDetails))
        {
            yield return result;
        }

        foreach (var result in ValidateNestedOptions(MergedPullRequests))
        {
            yield return result;
        }

        foreach (var result in ValidateNestedOptions(Telemetry))
        {
            yield return result;
        }
    }

    private IEnumerable<ValidationResult> ValidateRequiredBitbucketCredentials()
    {
        if (string.IsNullOrWhiteSpace(Workspace))
        {
            yield return new ValidationResult("Bitbucket workspace is required when DemoMode is false.", [nameof(Workspace)]);
        }

        if (string.IsNullOrWhiteSpace(AuthEmail))
        {
            yield return new ValidationResult("Bitbucket auth email is required when DemoMode is false.", [nameof(AuthEmail)]);
        }

        if (string.IsNullOrWhiteSpace(AuthApiToken))
        {
            yield return new ValidationResult("Bitbucket auth API token is required when DemoMode is false.", [nameof(AuthApiToken)]);
        }
    }

    private static IEnumerable<ValidationResult> ValidateNestedOptions(object? options)
    {
        if (options is null)
        {
            yield break;
        }

        var nestedResults = new List<ValidationResult>();
        _ = Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            nestedResults,
            validateAllProperties: true);

        foreach (var result in nestedResults)
        {
            yield return result;
        }
    }
}
