using System.Text.Json.Serialization;

namespace Yaof.Models;
/// <summary>
/// Represents unordered list item.
/// </summary>

public record UnorderedListItem : ContentItem
{
    /// <summary>
    /// Gets the type identifier for unordered list items.
    /// </summary>
    [JsonIgnore]
    public override string Type => "unorderedList";
    /// <summary>
    /// Gets or sets the items.
    /// </summary>

    [JsonPropertyName("items")]
    public required IReadOnlyList<string> Items { get; init; }
}
