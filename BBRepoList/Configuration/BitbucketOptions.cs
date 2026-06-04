using System.ComponentModel.DataAnnotations;

namespace BBRepoList.Configuration;

/// <summary>
/// Configuration settings for Bitbucket API access.
/// </summary>
public sealed class BitbucketOptions
    : IValidatableObject
{
    /// <summary>
    /// Base Bitbucket API URL.
    /// </summary>
    [Required]
    public required Uri BaseUrl { get; init; }

    /// <summary>
    /// Bitbucket workspace identifier.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string Workspace { get; init; }

    /// <summary>
    /// Authentication email for Bitbucket API.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string AuthEmail { get; init; }

    /// <summary>
    /// Authentication API token for Bitbucket API.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string AuthApiToken { get; init; }

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
