using System.Text.Json.Serialization;

namespace Yaof.Models;

/// <summary>Root object of the YAOF replacement JSON format.</summary>
public record ReplacementDocument
{
    /// <summary>
    /// Gets or sets the replacements.
    /// </summary>
    [JsonPropertyName("replacements")]
    public required IReadOnlyDictionary<string, ContentBlock> Replacements { get; init; }
}
