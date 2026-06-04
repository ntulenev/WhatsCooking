using System.Text.Json;
using System.Text.Json.Serialization;

namespace BBRepoList.Models;

/// <summary>
/// Converts pull request identifiers to and from their numeric JSON representation.
/// </summary>
public sealed class PullRequestIdJsonConverter : JsonConverter<PullRequestId>
{
    /// <inheritdoc />
    public override PullRequestId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        try
        {
            return new PullRequestId(reader.GetInt32());
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new JsonException("Pull request id must be greater than zero.", ex);
        }
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        PullRequestId value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteNumberValue(value.Value);
    }
}
