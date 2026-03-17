using System.Text.RegularExpressions;
using Api.Models;

namespace Api.Services;

/// <summary>
/// Parses changelog markdown in the format "## [vx.x.x] - yyyy-mm-dd" with sections ### Added, ### Changed, ### Removed, ### Fixed.
/// </summary>
public sealed class ChangelogParser : IChangelogParser
{
    private static readonly Regex VersionLineRegex = new(
        @"^\s*##\s*\[\s*v?([^\]]+)\s*\]\s*-\s*(\d{4}-\d{2}-\d{2})\s*$",
        RegexOptions.Compiled);

    private static readonly string[] SectionHeaders = ["### Added", "### Changed", "### Removed", "### Fixed"];

    /// <inheritdoc />
    public ChangelogParseResult Parse(string changelog)
    {
        if (string.IsNullOrWhiteSpace(changelog))
            return new ChangelogParseResult { Success = false, Error = "Changelog is empty." };

        var lines = changelog.Split('\n');
        string? version = null;
        string? date = null;
        var added = new List<string>();
        var changed = new List<string>();
        var removed = new List<string>();
        var fixed_ = new List<string>();

        string? currentSection = null;
        var started = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            var versionMatch = VersionLineRegex.Match(trimmed);
            if (versionMatch.Success)
            {
                if (started)
                    break;

                version = versionMatch.Groups[1].Value.Trim();
                date = versionMatch.Groups[2].Value;
                currentSection = null;
                started = true;
                continue;
            }

            if (version is null)
                continue;

            var section = SectionHeaders.FirstOrDefault(s =>
                trimmed.StartsWith(s, StringComparison.OrdinalIgnoreCase));
            if (section is not null)
            {
                currentSection = section;
                continue;
            }

            if (currentSection is null)
                continue;

            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                var item = trimmed["- ".Length..].Trim();
                if (item.Length > 0)
                {
                    if (currentSection.Equals("### Added", StringComparison.OrdinalIgnoreCase))
                        added.Add(item);
                    else if (currentSection.Equals("### Changed", StringComparison.OrdinalIgnoreCase))
                        changed.Add(item);
                    else if (currentSection.Equals("### Removed", StringComparison.OrdinalIgnoreCase))
                        removed.Add(item);
                    else if (currentSection.Equals("### Fixed", StringComparison.OrdinalIgnoreCase))
                        fixed_.Add(item);
                }
            }
        }

        if (version is null || date is null)
            return new ChangelogParseResult { Success = false, Error = "Version and date header (## [vx.x.x] - yyyy-mm-dd) not found." };

        return new ChangelogParseResult
        {
            Success = true,
            Data = new ParsedChangelog
            {
                Version = version,
                Date = date,
                Added = added,
                Changed = changed,
                Removed = removed,
                Fixed = fixed_
            }
        };
    }
}
