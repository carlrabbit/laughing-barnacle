using System.Text.Json.Serialization;

namespace Yaof.Models;

public record OrderedListItem : ContentItem
{
    [JsonIgnore]
    public override string Type => "orderedList";

    [JsonPropertyName("items")]
    public required IReadOnlyList<string> Items { get; init; }
}
