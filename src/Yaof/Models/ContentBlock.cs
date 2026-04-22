using System.Text.Json.Serialization;

namespace Yaof.Models;

/// <summary>
/// Documentation.
/// </summary>
public record ContentBlock
{
    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonPropertyName("items")]
    public required IReadOnlyList<ContentItem> Items { get; init; }
}
