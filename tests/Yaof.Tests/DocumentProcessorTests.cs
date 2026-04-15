using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Yaof;
using Yaof.Models;

namespace Yaof.Tests;

public class DocumentProcessorTests
{
    private readonly DocumentProcessor _sut = new();

    private static Stream CreateMinimalDocx()
    {
        var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(
                new Body(
                    new Paragraph(
                        new ParagraphProperties(
                            new ParagraphStyleId { Val = "Heading1" }),
                        new Run(new Text("Introduction {{repl:##intro}}"))),
                    new Paragraph(
                        new ParagraphProperties(
                            new ParagraphStyleId { Val = "Heading2" }),
                        new Run(new Text("Normal heading without marker"))),
                    new Paragraph(new Run(new Text("Some body text")))));
        }
        stream.Position = 0;
        return stream;
    }

    private static ReplacementDocument BuildReplacements(string id, params ContentItem[] items) =>
        new()
        {
            Replacements = new Dictionary<string, ContentBlock>
            {
                [id] = new ContentBlock { Items = items }
            }
        };

    [Test]
    public async Task ProcessDocument_WithMarkerInHeading_RemovesMarkerText()
    {
        // Arrange
        using Stream input = CreateMinimalDocx();
        using var output = new MemoryStream();
        ReplacementDocument replacements = BuildReplacements("intro",
            new ParagraphItem { Text = "This is the intro." });

        // Act
        _sut.ProcessDocument(input, replacements, output);

        // Assert — the heading text must no longer contain the marker
        output.Position = 0;
        using var result = WordprocessingDocument.Open(output, isEditable: false);
        var headings = result.MainDocumentPart!.Document.Body!
            .Elements<Paragraph>()
            .Where(p => p.ParagraphProperties?.ParagraphStyleId?.Val?.Value == "Heading1")
            .ToList();

        await Assert.That(headings).IsNotEmpty();
        string headingText = string.Concat(headings[0].Descendants<Text>().Select(t => t.Text));
        await Assert.That(headingText.Contains("{{repl:##intro}}")).IsFalse();
    }

    [Test]
    public async Task ProcessDocument_WithMarkerInHeading_InsertsParagraphAfterHeading()
    {
        // Arrange
        using Stream input = CreateMinimalDocx();
        using var output = new MemoryStream();
        ReplacementDocument replacements = BuildReplacements("intro",
            new ParagraphItem { Text = "Inserted paragraph text." });

        // Act
        _sut.ProcessDocument(input, replacements, output);

        // Assert — a paragraph with the replacement text should exist in the body
        output.Position = 0;
        using var result = WordprocessingDocument.Open(output, isEditable: false);
        var paragraphs = result.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        bool found = paragraphs.Any(p =>
            string.Concat(p.Descendants<Text>().Select(t => t.Text))
                .Contains("Inserted paragraph text."));

        await Assert.That(found).IsTrue();
    }

    [Test]
    public async Task ProcessDocument_WithRelativeHeader_InsertsHeadingAtCorrectLevel()
    {
        // Arrange
        using Stream input = CreateMinimalDocx();
        using var output = new MemoryStream();
        // Marker is at Heading1, relative level 1 → should insert at Heading2
        ReplacementDocument replacements = BuildReplacements("intro",
            new HeaderItem { Level = 1, Text = "Sub-heading" });

        // Act
        _sut.ProcessDocument(input, replacements, output);

        // Assert
        output.Position = 0;
        using var result = WordprocessingDocument.Open(output, isEditable: false);
        bool found = result.MainDocumentPart!.Document.Body!
            .Elements<Paragraph>()
            .Any(p => p.ParagraphProperties?.ParagraphStyleId?.Val?.Value == "Heading2"
                   && string.Concat(p.Descendants<Text>().Select(t => t.Text)) == "Sub-heading");

        await Assert.That(found).IsTrue();
    }

    [Test]
    public async Task ProcessDocument_WithUnknownId_LeavesDocumentUnchanged()
    {
        // Arrange
        using Stream input = CreateMinimalDocx();
        using var output = new MemoryStream();
        // Provide a replacement for a different ID
        ReplacementDocument replacements = BuildReplacements("other",
            new ParagraphItem { Text = "Should not appear." });

        // Act
        _sut.ProcessDocument(input, replacements, output);

        // Assert — marker text still present because no replacement was found for "intro"
        output.Position = 0;
        using var result = WordprocessingDocument.Open(output, isEditable: false);
        bool markerStillPresent = result.MainDocumentPart!.Document.Body!
            .Elements<Paragraph>()
            .Any(p => string.Concat(p.Descendants<Text>().Select(t => t.Text))
                         .Contains("{{repl:##intro}}"));

        await Assert.That(markerStillPresent).IsTrue();
    }

    [Test]
    public async Task ProcessDocument_WithUnorderedList_InsertsListBulletParagraphs()
    {
        // Arrange
        using Stream input = CreateMinimalDocx();
        using var output = new MemoryStream();
        ReplacementDocument replacements = BuildReplacements("intro",
            new UnorderedListItem { Items = ["Alpha", "Beta", "Gamma"] });

        // Act
        _sut.ProcessDocument(input, replacements, output);

        // Assert
        output.Position = 0;
        using var result = WordprocessingDocument.Open(output, isEditable: false);
        var listParagraphs = result.MainDocumentPart!.Document.Body!
            .Elements<Paragraph>()
            .Where(p => p.ParagraphProperties?.ParagraphStyleId?.Val?.Value == "ListBullet")
            .ToList();

        await Assert.That(listParagraphs.Count).IsEqualTo(3);
    }

    [Test]
    public async Task ProcessDocument_WithOrderedList_InsertsListNumberParagraphs()
    {
        // Arrange
        using Stream input = CreateMinimalDocx();
        using var output = new MemoryStream();
        ReplacementDocument replacements = BuildReplacements("intro",
            new OrderedListItem { Items = ["First", "Second"] });

        // Act
        _sut.ProcessDocument(input, replacements, output);

        // Assert
        output.Position = 0;
        using var result = WordprocessingDocument.Open(output, isEditable: false);
        var listParagraphs = result.MainDocumentPart!.Document.Body!
            .Elements<Paragraph>()
            .Where(p => p.ParagraphProperties?.ParagraphStyleId?.Val?.Value == "ListNumber")
            .ToList();

        await Assert.That(listParagraphs.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ProcessDocument_WithTableItem_InsertsTableWithHeaderAndRows()
    {
        // Arrange
        using Stream input = CreateMinimalDocx();
        using var output = new MemoryStream();
        ReplacementDocument replacements = BuildReplacements("intro",
            new TableItem
            {
                Header = ["Name", "Score"],
                Rows = [["Alice", "10"], ["Bob", "20"]]
            });

        // Act
        _sut.ProcessDocument(input, replacements, output);

        // Assert
        output.Position = 0;
        using var result = WordprocessingDocument.Open(output, isEditable: false);
        Table? table = result.MainDocumentPart!.Document.Body!.Elements<Table>().FirstOrDefault();
        await Assert.That(table).IsNotNull();

        List<TableRow> rows = table!.Elements<TableRow>().ToList();
        await Assert.That(rows.Count).IsEqualTo(3);
        await Assert.That(string.Concat(rows[0].Descendants<Text>().Select(t => t.Text))).IsEqualTo("NameScore");
        await Assert.That(string.Concat(rows[1].Descendants<Text>().Select(t => t.Text))).IsEqualTo("Alice10");
        await Assert.That(string.Concat(rows[2].Descendants<Text>().Select(t => t.Text))).IsEqualTo("Bob20");
    }

    [Test]
    public async Task ProcessDocument_WithTableStyle_SetsTableStyleReference()
    {
        // Arrange
        using Stream input = CreateMinimalDocx();
        using var output = new MemoryStream();
        ReplacementDocument replacements = BuildReplacements("intro",
            new TableItem
            {
                Rows = [["A"]],
                TableStyle = "TableGrid"
            });

        // Act
        _sut.ProcessDocument(input, replacements, output);

        // Assert
        output.Position = 0;
        using var result = WordprocessingDocument.Open(output, isEditable: false);
        Table? table = result.MainDocumentPart!.Document.Body!.Elements<Table>().FirstOrDefault();
        await Assert.That(table).IsNotNull();

        string? style = table!.GetFirstChild<TableProperties>()?.GetFirstChild<TableStyle>()?.Val?.Value;
        await Assert.That(style).IsEqualTo("TableGrid");
    }
}
