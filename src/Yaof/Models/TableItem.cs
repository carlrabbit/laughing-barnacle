using System.Text.Json.Serialization;

namespace Yaof.Models;

/// <summary>
/// Documentation.
/// </summary>
public record TableItem : ContentItem
{
    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonIgnore]
    public override string Type => "table";

    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonPropertyName("header")]
    public IReadOnlyList<string>? Header { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonPropertyName("rows")]
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonPropertyName("tableStyle")]
    public string? TableStyle { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    [JsonPropertyName("caption")]
    public string? Caption { get; init; }
}
