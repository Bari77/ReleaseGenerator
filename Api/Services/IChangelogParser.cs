namespace Api.Services;

/// <summary>
/// Parses raw changelog markdown into a structured result with version, date, and section items.
/// </summary>
public interface IChangelogParser
{
    /// <summary>
    /// Parses the given changelog text and returns a result indicating success or failure and the parsed data or error message.
    /// </summary>
    /// <param name="changelog">Raw changelog markdown (e.g. content of CHANGELOG.md).</param>
    /// <returns>A result with <see cref="ChangelogParseResult.Success"/>, and either <see cref="ChangelogParseResult.Data"/> or <see cref="ChangelogParseResult.Error"/>.</returns>
    ChangelogParseResult Parse(string changelog);
}
