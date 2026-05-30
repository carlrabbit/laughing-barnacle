using System.Text.Json.Serialization;

namespace Yaof.Models;
/// <summary>
/// Represents paragraph item.
/// </summary>

public record ParagraphItem : ContentItem
{
    /// <summary>
    /// Performs the json property name operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    [JsonIgnore]
    public override string Type => "paragraph";
    /// <summary>
    /// Gets or sets the text.
    /// </summary>

    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
