using System.Text.Json.Serialization;

namespace Yaof.Models;

/// <summary>
/// Documentation.
/// </summary>
public record UnorderedListItem : ContentItem
{
    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonIgnore]
    public override string Type => "unorderedList";

    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonPropertyName("items")]
    public required IReadOnlyList<string> Items { get; init; }
}
