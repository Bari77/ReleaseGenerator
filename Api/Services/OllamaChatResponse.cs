namespace Api.Services;

/// <summary>
/// Response body from the Ollama/OpenAI-compatible chat completions API.
/// </summary>
public sealed class OllamaChatResponse
{
    /// <summary>
    /// List of completion choices; the first choice's message content is used as the generated release note.
    /// </summary>
    public List<OllamaChoice>? Choices { get; set; }
}
