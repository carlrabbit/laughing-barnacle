using System.Text.Json.Serialization;

namespace Yaof.Models;

public record ParagraphItem : ContentItem
{
    [JsonIgnore]
    public override string Type => "paragraph";

    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
