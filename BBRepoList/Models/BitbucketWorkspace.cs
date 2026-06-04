namespace BBRepoList.Models;

/// <summary>
/// Bitbucket workspace identifier.
/// </summary>
public readonly record struct BitbucketWorkspace
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketWorkspace"/> struct.
    /// </summary>
    /// <param name="value">Workspace identifier value.</param>
    public BitbucketWorkspace(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Value = value.Trim();
    }

    /// <summary>
    /// Workspace identifier value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}
