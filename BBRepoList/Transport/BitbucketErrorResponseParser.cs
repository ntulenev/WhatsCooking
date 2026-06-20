using System.Net;
using System.Text;

using BBRepoList.Abstractions;

namespace BBRepoList.Transport;

/// <summary>
/// Parses Bitbucket HTTP error response bodies.
/// </summary>
public sealed class BitbucketErrorResponseParser : IBitbucketErrorResponseParser
{
    /// <inheritdoc />
    public string Parse(string body, string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        return IsHtmlResponse(body, mediaType)
            ? FormatHtmlResponseBody(body)
            : body;
    }

    private static bool IsHtmlResponse(string body, string? mediaType) =>
        string.Equals(mediaType, "text/html", StringComparison.OrdinalIgnoreCase)
        || body.Contains("<html", StringComparison.OrdinalIgnoreCase)
        || body.Contains("<!doctype html", StringComparison.OrdinalIgnoreCase);

    private static string FormatHtmlResponseBody(string body)
    {
        var text = WebUtility.HtmlDecode(RemoveHtmlTags(body));
        var lines = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static line => !string.IsNullOrWhiteSpace(line));

        return "HTML error response: " + string.Join(Environment.NewLine, lines);
    }

    private static string RemoveHtmlTags(string html)
    {
        var result = new StringBuilder(html.Length);
        var isInsideTag = false;

        for (var index = 0; index < html.Length; index++)
        {
            var character = html[index];
            if (character == '<')
            {
                AppendTagBoundary(result, html, index);
                isInsideTag = true;
                continue;
            }

            if (character == '>')
            {
                isInsideTag = false;
                continue;
            }

            if (!isInsideTag)
            {
                _ = result.Append(character);
            }
        }

        return result.ToString();
    }

    private static void AppendTagBoundary(StringBuilder result, string html, int tagStartIndex)
    {
        if (IsBlockTag(html, tagStartIndex))
        {
            _ = result.AppendLine();
        }
    }

    private static bool IsBlockTag(string html, int tagStartIndex) =>
        StartsWithTagName(html, tagStartIndex, "br")
        || StartsWithTagName(html, tagStartIndex, "p")
        || StartsWithTagName(html, tagStartIndex, "h1")
        || StartsWithTagName(html, tagStartIndex, "h2")
        || StartsWithTagName(html, tagStartIndex, "h3")
        || StartsWithTagName(html, tagStartIndex, "title")
        || StartsWithTagName(html, tagStartIndex, "pre")
        || StartsWithTagName(html, tagStartIndex, "body")
        || StartsWithTagName(html, tagStartIndex, "hr");

    private static bool StartsWithTagName(string html, int tagStartIndex, string tagName)
    {
        var nameStartIndex = tagStartIndex + 1;
        if (nameStartIndex < html.Length && html[nameStartIndex] == '/')
        {
            nameStartIndex++;
        }

        if (nameStartIndex + tagName.Length > html.Length)
        {
            return false;
        }

        if (!html.AsSpan(nameStartIndex, tagName.Length).Equals(tagName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var nameEndIndex = nameStartIndex + tagName.Length;
        return nameEndIndex == html.Length
               || html[nameEndIndex] is '>' or '/' or ' ' or '\t' or '\r' or '\n';
    }
}
