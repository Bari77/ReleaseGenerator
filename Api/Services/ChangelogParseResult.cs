using Api.Models;

namespace Api.Services;

/// <summary>
/// Result of parsing a changelog: either success with parsed data or failure with an error message.
/// </summary>
public sealed class ChangelogParseResult
{
    /// <summary>
    /// Whether the changelog was parsed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The parsed changelog when <see cref="Success"/> is true; otherwise null.
    /// </summary>
    public ParsedChangelog? Data { get; init; }

    /// <summary>
    /// Error description when <see cref="Success"/> is false; otherwise null.
    /// </summary>
    public string? Error { get; init; }
}
