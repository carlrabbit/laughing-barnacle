using System.Text.Json.Serialization;

namespace Yaof.Models;

/// <summary>
/// Represents a polymorphic base content item within a YAOF replacement document section.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HeaderItem), "header")]
[JsonDerivedType(typeof(ParagraphItem), "paragraph")]
[JsonDerivedType(typeof(ImageItem), "image")]
[JsonDerivedType(typeof(UnorderedListItem), "unorderedList")]
[JsonDerivedType(typeof(OrderedListItem), "orderedList")]
[JsonDerivedType(typeof(TableItem), "table")]
public abstract record ContentItem
{
    /// <summary>
    /// Gets the discriminator value used for polymorphic JSON serialization.
    /// </summary>
    public abstract string Type { get; }
}
