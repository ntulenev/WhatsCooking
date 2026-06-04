using System.Globalization;
using System.Text.Json.Serialization;

namespace BBRepoList.Models;

/// <summary>
/// Pull request identifier within repository scope.
/// </summary>
[JsonConverter(typeof(PullRequestIdJsonConverter))]
public readonly record struct PullRequestId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestId"/> struct.
    /// </summary>
    /// <param name="value">Positive pull request identifier.</param>
    public PullRequestId(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Pull request id must be greater than zero.");
        }

        Value = value;
    }

    /// <summary>
    /// Positive pull request identifier value.
    /// </summary>
    public int Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
