using System.Text.Json.Serialization;

namespace Yaof.Models;
/// <summary>
/// Represents ordered list item.
/// </summary>

public record OrderedListItem : ContentItem
{
    /// <summary>
    /// Performs the json property name operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    [JsonIgnore]
    public override string Type => "orderedList";
    /// <summary>
    /// Gets or sets the items.
    /// </summary>

    [JsonPropertyName("items")]
    public required IReadOnlyList<string> Items { get; init; }
}
