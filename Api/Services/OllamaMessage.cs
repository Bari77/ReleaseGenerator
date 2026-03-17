namespace Api.Services;

/// <summary>
/// A single message in an Ollama/OpenAI chat request or response.
/// </summary>
public sealed class OllamaMessage
{
    /// <summary>
    /// Role of the message (e.g. "user", "assistant", "system").
    /// </summary>
    public string Role { get; set; } = "";

    /// <summary>
    /// Message content (plain text).
    /// </summary>
    public string Content { get; set; } = "";
}
