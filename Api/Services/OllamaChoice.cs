namespace Api.Services;

/// <summary>
/// A single choice in an Ollama chat completion response.
/// </summary>
public sealed class OllamaChoice
{
    /// <summary>
    /// The assistant message containing the generated text.
    /// </summary>
    public OllamaMessage? Message { get; set; }
}
