namespace BBRepoList.Models;

/// <summary>
/// Repository slug within Bitbucket workspace scope.
/// </summary>
public readonly record struct RepositorySlug
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositorySlug"/> struct.
    /// </summary>
    /// <param name="value">Repository slug value.</param>
    public RepositorySlug(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Value = value.Trim();
    }

    /// <summary>
    /// Repository slug value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}
