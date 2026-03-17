namespace Api.Configuration;

/// <summary>
/// Options for a single output format (e.g. Discord, email), including the example template used to guide the model.
/// </summary>
public sealed class FormatOptions
{
    /// <summary>
    /// Example message template for this format. The model is instructed to follow this style; placeholders like {version} and {date} are replaced with actual values.
    /// </summary>
    public string ExampleTemplate { get; set; } = string.Empty;
}
