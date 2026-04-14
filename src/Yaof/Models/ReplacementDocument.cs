using System.Text.Json.Serialization;

namespace Yaof.Models;

/// <summary>Root object of the YAOF replacement JSON format.</summary>
public record ReplacementDocument
{
    [JsonPropertyName("replacements")]
    public required IReadOnlyDictionary<string, ContentBlock> Replacements { get; init; }
}
