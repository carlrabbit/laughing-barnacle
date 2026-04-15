using System.Text.Json;
using Yaof.Models;

namespace Yaof.Tests;

public class ReplacementDocumentSerializationTests
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    [Test]
    public async Task Deserialize_WithAllItemTypes_ProducesCorrectModelGraph()
    {
        // Arrange
        string json = """
            {
              "replacements": {
                "section1": {
                  "items": [
                    { "type": "header",        "level": 2,  "text": "Title" },
                    { "type": "paragraph",                  "text": "Body" },
                    { "type": "image",          "path": "img.png", "caption": "Cap" },
                    { "type": "unorderedList",  "items": ["A", "B"] },
                    { "type": "orderedList",    "items": ["1", "2"] },
                    { "type": "table",          "header": ["H1", "H2"], "rows": [["A1", "B1"]], "tableStyle": "TableGrid", "caption": "Table caption" }
                  ]
                }
              }
            }
            """;

        // Act
        ReplacementDocument? doc = JsonSerializer.Deserialize<ReplacementDocument>(json, Options);

        // Assert
        await Assert.That(doc).IsNotNull();
        await Assert.That(doc!.Replacements.ContainsKey("section1")).IsTrue();

        IReadOnlyList<ContentItem> items = doc.Replacements["section1"].Items;
        await Assert.That(items.Count).IsEqualTo(6);
        await Assert.That(items[0]).IsTypeOf<HeaderItem>();
        await Assert.That(items[1]).IsTypeOf<ParagraphItem>();
        await Assert.That(items[2]).IsTypeOf<ImageItem>();
        await Assert.That(items[3]).IsTypeOf<UnorderedListItem>();
        await Assert.That(items[4]).IsTypeOf<OrderedListItem>();
        await Assert.That(items[5]).IsTypeOf<TableItem>();
    }

    [Test]
    public async Task Deserialize_HeaderItem_HasCorrectProperties()
    {
        // Arrange
        string json = """
            {
              "replacements": {
                "s": { "items": [{ "type": "header", "level": 3, "text": "My heading" }] }
              }
            }
            """;

        // Act
        ReplacementDocument? doc = JsonSerializer.Deserialize<ReplacementDocument>(json, Options);
        var header = (HeaderItem)doc!.Replacements["s"].Items[0];

        // Assert
        await Assert.That(header.Level).IsEqualTo(3);
        await Assert.That(header.Text).IsEqualTo("My heading");
    }

    [Test]
    public async Task Deserialize_ImageItem_WithOptionalCaption_Works()
    {
        // Arrange
        string json = """
            {
              "replacements": {
                "s": { "items": [{ "type": "image", "path": "photo.jpg" }] }
              }
            }
            """;

        // Act
        ReplacementDocument? doc = JsonSerializer.Deserialize<ReplacementDocument>(json, Options);
        var img = (ImageItem)doc!.Replacements["s"].Items[0];

        // Assert
        await Assert.That(img.Path).IsEqualTo("photo.jpg");
        await Assert.That(img.Caption).IsNull();
    }

    [Test]
    public async Task Deserialize_TableItem_WithHeaderAndStyle_HasCorrectProperties()
    {
        // Arrange
        string json = """
            {
              "replacements": {
                "s": {
                  "items": [
                    {
                      "type": "table",
                      "header": ["Name", "Value"],
                      "rows": [["A", "1"], ["B", "2"]],
                      "tableStyle": "TableGrid"
                    }
                  ]
                }
              }
            }
            """;

        // Act
        ReplacementDocument? doc = JsonSerializer.Deserialize<ReplacementDocument>(json, Options);
        var table = (TableItem)doc!.Replacements["s"].Items[0];

        // Assert
        await Assert.That(table.Header).IsNotNull();
        await Assert.That(table.Header!.Count).IsEqualTo(2);
        await Assert.That(table.Rows.Count).IsEqualTo(2);
        await Assert.That(table.Rows[0][0]).IsEqualTo("A");
        await Assert.That(table.TableStyle).IsEqualTo("TableGrid");
        await Assert.That(table.Caption).IsNull();
    }

    [Test]
    public async Task Deserialize_TableItem_WithCaption_HasCaption()
    {
        // Arrange
        string json = """
            {
              "replacements": {
                "s": {
                  "items": [
                    {
                      "type": "table",
                      "rows": [["A"]],
                      "caption": "Results table"
                    }
                  ]
                }
              }
            }
            """;

        // Act
        ReplacementDocument? doc = JsonSerializer.Deserialize<ReplacementDocument>(json, Options);
        var table = (TableItem)doc!.Replacements["s"].Items[0];

        // Assert
        await Assert.That(table.Caption).IsEqualTo("Results table");
    }
}
