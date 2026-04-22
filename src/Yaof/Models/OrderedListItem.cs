using System.Text.Json.Serialization;

namespace Yaof.Models;

/// <summary>
/// Documentation.
/// </summary>
public record OrderedListItem : ContentItem
{
    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonIgnore]
    public override string Type => "orderedList";

    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonPropertyName("items")]
    public required IReadOnlyList<string> Items { get; init; }
}
