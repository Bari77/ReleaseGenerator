namespace Api.Models;

/// <summary>
/// Structured representation of a parsed changelog: version, date, and items grouped by section (Added, Changed, Removed, Fixed).
/// </summary>
public sealed class ParsedChangelog
{
    /// <summary>
    /// Version string parsed from the changelog header (e.g. "1.0.0" or "v1.0.0").
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Release date in yyyy-mm-dd format.
    /// </summary>
    public required string Date { get; init; }

    /// <summary>
    /// Items listed under the "### Added" section.
    /// </summary>
    public IReadOnlyList<string> Added { get; init; } = [];

    /// <summary>
    /// Items listed under the "### Changed" section.
    /// </summary>
    public IReadOnlyList<string> Changed { get; init; } = [];

    /// <summary>
    /// Items listed under the "### Removed" section.
    /// </summary>
    public IReadOnlyList<string> Removed { get; init; } = [];

    /// <summary>
    /// Items listed under the "### Fixed" section.
    /// </summary>
    public IReadOnlyList<string> Fixed { get; init; } = [];
}
