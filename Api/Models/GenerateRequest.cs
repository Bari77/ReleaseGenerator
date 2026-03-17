namespace Api.Models;

/// <summary>
/// Request body for the generate endpoint: raw changelog text plus target format and language.
/// </summary>
public sealed class GenerateRequest
{
    /// <summary>
    /// Raw changelog markdown content (e.g. full CHANGELOG.md text).
    /// </summary>
    public required string Changelog { get; init; }

    /// <summary>
    /// Output format identifier (must match a key in ReleaseGenerator:Formats configuration).
    /// </summary>
    public required string Format { get; init; }

    /// <summary>
    /// Language code for the generated note (must be in ReleaseGenerator:Languages).
    /// </summary>
    public required string Language { get; init; }
}
