namespace Api.Configuration;

/// <summary>
/// Configuration for the Ollama API used to generate release notes.
/// </summary>
public sealed class OllamaOptions
{
    /// <summary>
    /// Base URL of the Ollama server (e.g. http://localhost:11434 or http://ollama:11434 in Docker).
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Model name to use for chat completions (e.g. llama3.2).
    /// </summary>
    public string Model { get; set; } = "llama3.2";

    /// <summary>
    /// Request timeout in seconds when calling Ollama.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Optional global context to help the model understand the product, audience, tone, and constraints.
    /// This text is appended to the prompt and should be short and specific.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Optional glossary to enforce preferred terms or translations.
    /// The key is a source term; the value is the preferred output term in the target language.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Glossary { get; set; }
}
