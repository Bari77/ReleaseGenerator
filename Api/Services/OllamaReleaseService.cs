using Api.Configuration;
using Api.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Api.Services;

/// <summary>
/// Calls the Ollama OpenAI-compatible chat API to generate release notes from a parsed changelog and format template.
/// </summary>
/// <param name="httpClient">HTTP client used to call Ollama (base address and timeout are configured via <see cref="ReleaseGeneratorOptions"/>).</param>
/// <param name="options">ReleaseGenerator options containing Ollama URL, model, and timeout.</param>
public sealed class OllamaReleaseService(HttpClient httpClient, IOptions<ReleaseGeneratorOptions> options) : IOllamaReleaseService
{
    private readonly ReleaseGeneratorOptions _options = options.Value;

    /// <inheritdoc />
    public async Task<string> GenerateReleaseNoteAsync(ParsedChangelog parsed, string format, string language, string exampleTemplate, CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(parsed, format, language, exampleTemplate);
        var request = new OllamaChatRequest
        {
            Model = _options.Ollama.Model,
            Messages =
            [
                new OllamaMessage { Role = "user", Content = prompt }
            ],
            Stream = false
        };

        var baseUrl = _options.Ollama.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/v1/chat/completions";
        using var response = await httpClient.PostAsJsonAsync(url, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(jsonOptions, cancellationToken);
        var content = result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
        return content ?? string.Empty;
    }

    private static string BuildPrompt(ParsedChangelog parsed, string format, string language, string exampleTemplate)
    {
        var sections = new List<string>();
        if (parsed.Added.Count > 0)
            sections.Add("Added:\n" + string.Join("\n", parsed.Added.Select(x => "- " + x)));
        if (parsed.Changed.Count > 0)
            sections.Add("Changed:\n" + string.Join("\n", parsed.Changed.Select(x => "- " + x)));
        if (parsed.Removed.Count > 0)
            sections.Add("Removed:\n" + string.Join("\n", parsed.Removed.Select(x => "- " + x)));
        if (parsed.Fixed.Count > 0)
            sections.Add("Fixed:\n" + string.Join("\n", parsed.Fixed.Select(x => "- " + x)));

        var changelogText = string.Join("\n\n", sections);
        if (string.IsNullOrWhiteSpace(changelogText))
            changelogText = "(No items in this release)";

        var langInstruction = $"Write the release note in language code \"{language}\". Use a clear, user-friendly tone suitable for the target audience.";

        return "You are a release note writer. Given a technical changelog, produce a single release note for the \"" + format + "\" format.\n\n"
            + langInstruction + "\n\n"
            + "The output must follow the style and structure of this example (use placeholders like {version} and {date} only as reference; replace them with the actual version and date):\n\n"
            + exampleTemplate + "\n"
            + "\n\n"
            + "Technical changelog for version " + parsed.Version + " (" + parsed.Date + "):\n\n"
            + changelogText + "\n\n"
            + "Produce only the release note content, no preamble or explanation.";
    }
}
