using System.Text.Json.Serialization;

namespace Yaof.Models;
/// <summary>
/// Represents content block.
/// </summary>

public record ContentBlock
{
    /// <summary>
    /// Gets or sets the items.
    /// </summary>
    [JsonPropertyName("items")]
    public required IReadOnlyList<ContentItem> Items { get; init; }
}
