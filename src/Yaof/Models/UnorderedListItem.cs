using System.Text.Json.Serialization;

namespace Yaof.Models;

public record UnorderedListItem : ContentItem
{
    [JsonIgnore]
    public override string Type => "unorderedList";

    [JsonPropertyName("items")]
    public required IReadOnlyList<string> Items { get; init; }
}
