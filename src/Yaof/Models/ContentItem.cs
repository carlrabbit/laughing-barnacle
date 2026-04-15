using System.Text.Json.Serialization;

namespace Yaof.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HeaderItem), "header")]
[JsonDerivedType(typeof(ParagraphItem), "paragraph")]
[JsonDerivedType(typeof(ImageItem), "image")]
[JsonDerivedType(typeof(UnorderedListItem), "unorderedList")]
[JsonDerivedType(typeof(OrderedListItem), "orderedList")]
[JsonDerivedType(typeof(TableItem), "table")]
public abstract record ContentItem
{
    public abstract string Type { get; }
}
