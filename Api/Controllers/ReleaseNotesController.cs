using Api.Configuration;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Controllers;

/// <summary>
/// API controller for listing allowed formats and languages and for generating release notes from a changelog.
/// </summary>
/// <param name="parser">Changelog parser used to parse raw markdown.</param>
/// <param name="ollama">Service that calls Ollama to generate the release note text.</param>
/// <param name="options">ReleaseGenerator configuration (formats, languages).</param>
[ApiController]
[Route("api/[controller]")]
public class ReleaseNotesController(
    IChangelogParser parser,
    IOllamaReleaseService ollama,
    IOptions<ReleaseGeneratorOptions> options) : ControllerBase
{
    private readonly ReleaseGeneratorOptions _options = options.Value;

    /// <summary>
    /// Returns the list of configured output format identifiers (e.g. discord, email, appstores).
    /// </summary>
    /// <returns>JSON object with a "formats" array.</returns>
    [HttpGet("formats")]
    [Produces("application/json")]
    public IActionResult GetFormats()
    {
        var formatKeys = _options.Formats?.Keys.ToList() ?? [];
        return Ok(new { formats = formatKeys });
    }

    /// <summary>
    /// Returns the list of allowed language codes (e.g. en, fr).
    /// </summary>
    /// <returns>JSON object with a "languages" array.</returns>
    [HttpGet("languages")]
    [Produces("application/json")]
    public IActionResult GetLanguages()
    {
        var languages = _options.Languages ?? [];
        return Ok(new { languages });
    }

    /// <summary>
    /// Generates a release note from the provided JSON request body (changelog, format, language).
    /// Validates input against configuration, parses the changelog, then calls Ollama. Returns plain text by default; send Accept: application/json to get JSON.
    /// </summary>
    /// <param name="request">Request containing raw changelog text, format id, and language code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with generated content (text/plain by default, or JSON if Accept: application/json); 400 if validation or parsing fails.</returns>
    [HttpPost("generate")]
    [Consumes("application/json")]
    [Produces("text/plain", "application/json")]
    public async Task<IActionResult> Generate([FromBody] GenerateRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateRequest(request);
        if (validation is not null)
            return validation;

        var parseResult = parser.Parse(request.Changelog);
        if (!parseResult.Success)
            return BadRequest(new { error = parseResult.Error });

        var formatConfig = _options.Formats![request.Format];
        var exampleTemplate = formatConfig?.ExampleTemplate ?? string.Empty;

        var content = await ollama.GenerateReleaseNoteAsync(
            parseResult.Data!,
            request.Format,
            request.Language,
            exampleTemplate,
            cancellationToken);

        if (Request.Headers.Accept.Any(a => a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true))
            return Ok(new GenerateResponse { Content = content });

        return Content(content, "text/plain");
    }

    /// <summary>
    /// Generates a release note from the raw request body (changelog text) with format and language supplied as query parameters.
    /// Intended for CI/CD usage (e.g. curl -d @CHANGELOG.md).
    /// </summary>
    /// <param name="format">Output format identifier (query).</param>
    /// <param name="language">Language code (query).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Same as <see cref="Generate"/>: 200 with generated content or 400 on validation/parse error.</returns>
    [HttpPost("generate-from-body")]
    [Consumes("text/plain")]
    [Produces("text/plain", "application/json")]
    public async Task<IActionResult> GenerateFromBody(
        [FromQuery] string format,
        [FromQuery] string language,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var changelog = await reader.ReadToEndAsync(cancellationToken);
        var request = new GenerateRequest { Changelog = changelog, Format = format, Language = language };
        return await Generate(request, cancellationToken);
    }

    /// <summary>
    /// Validates the generate request against configuration (required fields, allowed format and language).
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>BadRequest result if invalid; null if valid.</returns>
    private IActionResult? ValidateRequest(GenerateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Changelog))
            return BadRequest(new { error = "Changelog is required." });

        if (string.IsNullOrWhiteSpace(request.Format))
            return BadRequest(new { error = "Format is required." });

        if (string.IsNullOrWhiteSpace(request.Language))
            return BadRequest(new { error = "Language is required." });

        var formats = _options.Formats;
        if (formats is null || !formats.ContainsKey(request.Format))
            return BadRequest(new { error = $"Unknown format: {request.Format}. Allowed: {string.Join(", ", formats?.Keys ?? [])}." });

        var languages = _options.Languages;
        if (languages is null || !languages.Contains(request.Language, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = $"Unknown language: {request.Language}. Allowed: {string.Join(", ", languages ?? [])}." });

        return null;
    }
}
