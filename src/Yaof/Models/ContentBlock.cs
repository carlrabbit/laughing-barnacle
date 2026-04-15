using System.Text.Json.Serialization;

namespace Yaof.Models;

public record ContentBlock
{
    [JsonPropertyName("items")]
    public required IReadOnlyList<ContentItem> Items { get; init; }
}
