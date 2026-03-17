namespace Api.Configuration;

/// <summary>
/// Root configuration options for the ReleaseGenerator API, bound from the "ReleaseGenerator" section.
/// </summary>
public sealed class ReleaseGeneratorOptions
{
    /// <summary>
    /// Configuration section name used in appsettings and environment variables.
    /// </summary>
    public const string SectionName = "ReleaseGenerator";

    /// <summary>
    /// Ollama connection and model settings.
    /// </summary>
    public OllamaOptions Ollama { get; set; } = new();

    /// <summary>
    /// Allowed language codes for generated release notes (e.g. "en", "fr").
    /// </summary>
    public IReadOnlyList<string> Languages { get; set; } = ["en", "fr"];

    /// <summary>
    /// Map of format identifier to format options (e.g. "discord", "email"), each containing an example template.
    /// </summary>
    public IReadOnlyDictionary<string, FormatOptions> Formats { get; set; } = new Dictionary<string, FormatOptions>();
}
