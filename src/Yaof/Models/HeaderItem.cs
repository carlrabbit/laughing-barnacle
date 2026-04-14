using System.Text.Json.Serialization;

namespace Yaof.Models;

public record HeaderItem : ContentItem
{
    [JsonIgnore]
    public override string Type => "header";

    /// <summary>Relative heading level offset (1 = one level deeper than the parent heading).</summary>
    [JsonPropertyName("level")]
    public required int Level { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
