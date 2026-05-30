using System.Text.Json.Serialization;

namespace Yaof.Models;
/// <summary>
/// Represents header item.
/// </summary>

public record HeaderItem : ContentItem
{
    /// <summary>
    /// Performs the offset operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    [JsonIgnore]
    public override string Type => "header";

    /// <summary>Relative heading level offset (1 = one level deeper than the parent heading).</summary>
    [JsonPropertyName("level")]
    public required int Level { get; init; }
    /// <summary>
    /// Gets or sets the text.
    /// </summary>

    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
