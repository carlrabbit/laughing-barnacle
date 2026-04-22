using System.Text.Json.Serialization;

namespace Yaof.Models;
/// <summary>
/// Represents table item.
/// </summary>

public record TableItem : ContentItem
{
    /// <summary>
    /// Gets the type identifier for table items.
    /// </summary>
    [JsonIgnore]
    public override string Type => "table";
    /// <summary>
    /// Performs the json property name operation.
    /// </summary>
    /// <returns>The operation result.</returns>

    [JsonPropertyName("header")]
    public IReadOnlyList<string>? Header { get; init; }
    /// <summary>
    /// Performs the json property name operation.
    /// </summary>
    /// <returns>The operation result.</returns>

    [JsonPropertyName("rows")]
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
    /// <summary>
    /// Performs the json property name operation.
    /// </summary>
    /// <returns>The operation result.</returns>

    [JsonPropertyName("tableStyle")]
    public string? TableStyle { get; init; }
    /// <summary>
    /// Gets or sets the caption.
    /// </summary>

    [JsonPropertyName("caption")]
    public string? Caption { get; init; }
}
