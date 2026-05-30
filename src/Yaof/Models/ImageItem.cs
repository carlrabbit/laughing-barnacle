using System.Text.Json.Serialization;

namespace Yaof.Models;
/// <summary>
/// Represents image item.
/// </summary>

public record ImageItem : ContentItem
{
    /// <summary>
    /// Performs the json property name operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    [JsonIgnore]
    public override string Type => "image";
    /// <summary>
    /// Performs the json property name operation.
    /// </summary>
    /// <returns>The operation result.</returns>

    [JsonPropertyName("path")]
    public required string Path { get; init; }
    /// <summary>
    /// Gets or sets the caption.
    /// </summary>

    [JsonPropertyName("caption")]
    public string? Caption { get; init; }
}
