namespace Api.Models;

/// <summary>
/// Response body for the generate endpoint, containing the generated release note text.
/// </summary>
public sealed class GenerateResponse
{
    /// <summary>
    /// The generated release note content, formatted according to the requested format and language.
    /// </summary>
    public required string Content { get; init; }
}
