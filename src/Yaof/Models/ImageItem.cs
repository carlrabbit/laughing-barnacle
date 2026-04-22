using System.Text.Json.Serialization;

namespace Yaof.Models;

/// <summary>
/// Documentation.
/// </summary>
public record ImageItem : ContentItem
{
    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonIgnore]
    public override string Type => "image";

    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonPropertyName("caption")]
    public string? Caption { get; init; }
}
