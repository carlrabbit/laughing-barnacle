using System.Text.Json.Serialization;

namespace Yaof.Models;

public record TableItem : ContentItem
{
    [JsonIgnore]
    public override string Type => "table";

    [JsonPropertyName("header")]
    public IReadOnlyList<string>? Header { get; init; }

    [JsonPropertyName("rows")]
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }

    [JsonPropertyName("tableStyle")]
    public string? TableStyle { get; init; }

    [JsonPropertyName("caption")]
    public string? Caption { get; init; }
}
