using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MDE;

namespace MDE.Tests;

public class MarkdownExtractorTests
{
    [Test]
    public async Task TryParse_WithOnlyInput_UsesDefaultOutputImageAndMappingPaths()
    {
        // Arrange
        string inputPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"mde-input-{Guid.NewGuid():N}.docx"));

        // Act
        bool success = MarkdownExtractorCliArguments.TryParse([inputPath], out MarkdownExtractorCliArguments? options, out string? _);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(options).IsNotNull();
        await Assert.That(options!.OutputFile).IsEqualTo(Path.ChangeExtension(inputPath, ".md"));
        await Assert.That(options.ImageDirectory).IsEqualTo($"{Path.Combine(Path.GetDirectoryName(inputPath) ?? string.Empty, Path.GetFileNameWithoutExtension(inputPath))}_imaged");
        await Assert.That(options.MappingFile).IsEqualTo($"{inputPath}.mapping.json");
    }

    [Test]
    public async Task Extract_WithHeadingParagraphAndUnknownStyle_WritesMarkdownAndMapping()
    {
        // Arrange
        string tempDirectory = Path.Combine(Path.GetTempPath(), $"mde-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            string inputPath = Path.Combine(tempDirectory, "input.docx");
            string outputPath = Path.Combine(tempDirectory, "output.md");
            string imageDirectory = Path.Combine(tempDirectory, "images");
            string mappingPath = Path.Combine(tempDirectory, "mapping.json");

            CreateDocument(inputPath);
            var sut = new MarkdownExtractor();

            // Act
            sut.Extract(inputPath, outputPath, imageDirectory, mappingPath);

            // Assert
            string markdown = await File.ReadAllTextAsync(outputPath);
            await Assert.That(markdown).Contains("# Intro");
            await Assert.That(markdown).Contains("Localized heading text");

            string mapping = await File.ReadAllTextAsync(mappingPath);
            await Assert.That(mapping).Contains("Markdown mapping targets");
            await Assert.That(mapping).Contains("\"Überschrift 1\": \"unknown\"");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static void CreateDocument(string path)
    {
        using WordprocessingDocument document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);

        MainDocumentPart mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body(
            new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
                new Run(new Text("Intro"))),
            new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = "CustomHeadingStyle" }),
                new Run(new Text("Localized heading text")))));

        StyleDefinitionsPart stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new Style
            {
                Type = StyleValues.Paragraph,
                StyleId = "Heading1",
                StyleName = new StyleName { Val = "Heading 1" }
            },
            new Style
            {
                Type = StyleValues.Paragraph,
                StyleId = "CustomHeadingStyle",
                StyleName = new StyleName { Val = "Überschrift 1" }
            });

        mainPart.Document.Save();
    }
}
