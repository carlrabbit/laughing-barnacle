using System.Text.Json.Serialization;

namespace Yaof.Models;

/// <summary>
/// Documentation.
/// </summary>
public record HeaderItem : ContentItem
{
    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonIgnore]
    public override string Type => "header";

    /// <summary>Relative heading level offset (1 = one level deeper than the parent heading).</summary>
    [JsonPropertyName("level")]
    public required int Level { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
