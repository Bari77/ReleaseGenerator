using Api.Models;

namespace Api.Services;

/// <summary>
/// Generates human-friendly release notes from a parsed changelog by calling an Ollama (OpenAI-compatible) API.
/// </summary>
public interface IOllamaReleaseService
{
    /// <summary>
    /// Calls Ollama to generate a release note in the given format and language, using the example template as a style guide.
    /// </summary>
    /// <param name="parsed">The parsed changelog data.</param>
    /// <param name="format">Target format identifier (e.g. "discord", "email").</param>
    /// <param name="language">Language code (e.g. "en", "fr").</param>
    /// <param name="exampleTemplate">Example message template the model should follow for style and structure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated release note text.</returns>
    Task<string> GenerateReleaseNoteAsync(ParsedChangelog parsed, string format, string language, string exampleTemplate, CancellationToken cancellationToken = default);
}
