using System.Text.Json.Serialization;

namespace Yaof.Models;

public record ImageItem : ContentItem
{
    [JsonIgnore]
    public override string Type => "image";

    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("caption")]
    public string? Caption { get; init; }
}
