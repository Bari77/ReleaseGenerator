namespace Api.Services;

/// <summary>
/// Request body for the Ollama/OpenAI-compatible chat completions API (v1/chat/completions).
/// </summary>
public sealed class OllamaChatRequest
{
    /// <summary>
    /// Model name (e.g. llama3.2).
    /// </summary>
    public string Model { get; set; } = "";

    /// <summary>
    /// Conversation messages; for release note generation, a single user message containing the prompt.
    /// </summary>
    public List<OllamaMessage> Messages { get; set; } = [];

    /// <summary>
    /// Whether to stream the response; set to false for a single completion.
    /// </summary>
    public bool Stream { get; set; }
}
