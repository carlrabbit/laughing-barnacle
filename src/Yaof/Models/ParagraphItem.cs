using System.Text.Json.Serialization;

namespace Yaof.Models;

/// <summary>
/// Documentation.
/// </summary>
public record ParagraphItem : ContentItem
{
    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonIgnore]
    public override string Type => "paragraph";

    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
