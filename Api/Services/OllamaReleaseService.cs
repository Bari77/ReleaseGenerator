using System.Text.RegularExpressions;
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
        var (prompt, wrapTag) = BuildPrompt(parsed, format, language, exampleTemplate, _options.Ollama.Context, _options.Ollama.Glossary);
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
        var content = result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;

        if (!string.IsNullOrEmpty(wrapTag))
            content = "<" + wrapTag + ">\n" + content + "\n</" + wrapTag + ">";

        return content;
    }

    private static (string Prompt, string? WrapTag) BuildPrompt(
        ParsedChangelog parsed,
        string format,
        string language,
        string exampleTemplate,
        string? context,
        IReadOnlyDictionary<string, string>? glossary)
    {
        var (templateForLanguage, templateIsForOtherLanguage, matchedTag) = GetTemplateForLanguage(exampleTemplate, language);
        string? wrapTag = format.Equals("store", StringComparison.OrdinalIgnoreCase) ? matchedTag : null;

        var allowedItems = new List<string>();
        foreach (var x in parsed.Added) allowedItems.Add("[Added] " + x);
        foreach (var x in parsed.Changed) allowedItems.Add("[Changed] " + x);
        foreach (var x in parsed.Removed) allowedItems.Add("[Removed] " + x);
        foreach (var x in parsed.Fixed) allowedItems.Add("[Fixed] " + x);

        var changelogSections = new List<string>();
        if (parsed.Added.Count > 0)
            changelogSections.Add("Added:\n" + string.Join("\n", parsed.Added.Select(x => "- " + x)));
        if (parsed.Changed.Count > 0)
            changelogSections.Add("Changed:\n" + string.Join("\n", parsed.Changed.Select(x => "- " + x)));
        if (parsed.Removed.Count > 0)
            changelogSections.Add("Removed:\n" + string.Join("\n", parsed.Removed.Select(x => "- " + x)));
        if (parsed.Fixed.Count > 0)
            changelogSections.Add("Fixed:\n" + string.Join("\n", parsed.Fixed.Select(x => "- " + x)));
        var changelogText = string.Join("\n\n", changelogSections);
        if (string.IsNullOrWhiteSpace(changelogText))
            changelogText = "(No items in this release)";

        var allowedList = allowedItems.Count > 0
            ? string.Join("\n", allowedItems.Select((item, i) => "  " + (i + 1) + ". " + item))
            : "  (none)";

        var sb = new System.Text.StringBuilder();
        sb.Append("You write release notes by filling in a template. You must change ONLY the placeholders; everything else must stay exactly as in the template.\n\n");

        if (!string.IsNullOrWhiteSpace(context))
        {
            sb.Append("CONTEXT (use to understand the product):\n");
            sb.Append(context.Trim()).Append("\n\n");
        }

        if (glossary is not null && glossary.Count > 0)
        {
            sb.Append("GLOSSARY (enforce these preferred terms in the output; keep proper nouns unchanged):\n");
            foreach (var entry in glossary)
                sb.Append("- ").Append(entry.Key).Append(" => ").Append(entry.Value).Append('\n');
            sb.Append('\n');
        }

        sb.Append("RULES:\n");
        sb.Append("1. Only replace these placeholders:\n");
        sb.Append("   - \"...\" or \"....\" (one or more dots): replace with the actual content for that section. Add one line per item from the list below (rephrased/vulgarized in the requested language \"").Append(language).Append("\"). Do not add or remove formatting.\n");
        sb.Append("   - \"{version}\": replace with \"").Append(parsed.Version).Append("\".\n");
        sb.Append("   - \"{date}\": replace with \"").Append(parsed.Date).Append("\".\n");
        sb.Append("2. If a section (Added/Ajoutés, Modified/Modifiés, Removed/Retirés, Fixed/Corrigés) has no item in the list below, do not include that section at all: omit the section header and its placeholder entirely.\n");
        sb.Append("3. Do not change anything else: keep every **, __, ###, and the structure of the sections you do output. Output language is \"").Append(language).Append("\".\n");
        sb.Append("4. You may ONLY use the following items. Put each in its correct section. For store format, vulgarize the wording for a general audience. Do not add any item not in this list:\n\n");
        sb.Append("ITEMS TO INCLUDE:\n");
        sb.Append(allowedList).Append("\n\n");
        sb.Append("5. Do not add \"---\", footers, or XML tags in the output (tags are added automatically when needed).\n\n");
        if (templateIsForOtherLanguage)
            sb.Append("(Template structure below; fill it in language \"").Append(language).Append("\".)\n\n");
        sb.Append("TEMPLATE (only replace ... and {version}/{date}):\n\n");
        sb.Append(templateForLanguage).Append("\n\n");
        sb.Append("SOURCE CHANGELOG:\n\n");
        sb.Append(changelogText).Append("\n\n");
        sb.Append("Output only the filled template, no preamble.");
        return (sb.ToString(), wrapTag);
    }

    private static (string Template, bool IsFromOtherLanguage, string? MatchedTag) GetTemplateForLanguage(string exampleTemplate, string language)
    {
        string[] tagCandidates = language.ToLowerInvariant() switch
        {
            "en" => ["en-US", "en_US", "en"],
            "fr" => ["fr-FR", "fr_FR", "fr"],
            _ => [language]
        };

        foreach (var tag in tagCandidates)
        {
            var pattern = @"<\s*" + Regex.Escape(tag) + @"\s*>([\s\S]*?)<\s*/\s*" + Regex.Escape(tag) + @"\s*>";
            var match = Regex.Match(exampleTemplate, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
                return (match.Groups[1].Value.Trim(), false, tag);
        }

        var firstBlock = Regex.Match(exampleTemplate, @"<\s*([\w-]+)\s*>([\s\S]*?)<\s*/\s*\1\s*>", RegexOptions.IgnoreCase);
        if (firstBlock.Success)
            return (firstBlock.Groups[2].Value.Trim(), true, null);

        return (exampleTemplate.Trim(), false, null);
    }
}
