using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Yaof.Models;

namespace Yaof;

/// <summary>Processes Open XML Word documents by replacing marker headings with structured content.</summary>
public sealed partial class DocumentProcessor : IDocumentProcessor
{
    private static readonly Regex MarkerRegex = CreateMarkerRegex();

    [GeneratedRegex(@"\{\{repl:##(?<id>[^}]+)\}\}", RegexOptions.Compiled)]
    private static partial Regex CreateMarkerRegex();

    /// <inheritdoc/>
    public void ProcessDocument(string docxPath, ReplacementDocument replacements, string outputPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(docxPath);
        ArgumentException.ThrowIfNullOrEmpty(outputPath);
        ArgumentNullException.ThrowIfNull(replacements);

        File.Copy(docxPath, outputPath, overwrite: true);
        using var document = WordprocessingDocument.Open(outputPath, isEditable: true);
        ApplyReplacements(document, replacements);
    }

    /// <inheritdoc/>
    public void ProcessDocument(Stream docxStream, ReplacementDocument replacements, Stream output)
    {
        ArgumentNullException.ThrowIfNull(docxStream);
        ArgumentNullException.ThrowIfNull(replacements);
        ArgumentNullException.ThrowIfNull(output);

        docxStream.CopyTo(output);
        output.Position = 0;
        using var document = WordprocessingDocument.Open(output, isEditable: true);
        ApplyReplacements(document, replacements);
    }

    private static void ApplyReplacements(WordprocessingDocument document, ReplacementDocument replacements)
    {
        Body body = document.MainDocumentPart?.Document?.Body
            ?? throw new InvalidOperationException("The document has no body.");

        // Collect all heading paragraphs with markers first (avoid modifying collection while iterating)
        var markers = FindMarkers(body);

        foreach (var (paragraph, id, headingLevel) in markers)
        {
            if (!replacements.Replacements.TryGetValue(id, out ContentBlock? block))
                continue;

            RemoveMarkerFromParagraph(paragraph, id);
            InsertContentAfterParagraph(document, paragraph, block, headingLevel);
        }
    }

    private static List<(Paragraph paragraph, string id, int headingLevel)> FindMarkers(Body body)
    {
        var result = new List<(Paragraph, string, int)>();

        foreach (Paragraph paragraph in body.Elements<Paragraph>())
        {
            int? level = GetHeadingLevel(paragraph);
            if (level is null)
                continue;

            string text = GetParagraphText(paragraph);
            Match match = MarkerRegex.Match(text);
            if (match.Success)
            {
                result.Add((paragraph, match.Groups["id"].Value, level.Value));
            }
        }

        return result;
    }

    private static int? GetHeadingLevel(Paragraph paragraph)
    {
        string? styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (styleId is null) return null;

        // Standard Word style IDs: Heading1..Heading9
        if (styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(styleId["Heading".Length..], out int level) && level is >= 1 and <= 9)
                return level;
        }

        return null;
    }

    private static string GetParagraphText(Paragraph paragraph) =>
        string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));

    private static string BuildMarkerText(string id) => $"{{{{repl:##{id}}}}}";

    private static void RemoveMarkerFromParagraph(Paragraph paragraph, string id)
    {
        string markerText = BuildMarkerText(id);

        List<Run> runs = paragraph.Elements<Run>().ToList();
        string full = string.Concat(runs.Select(r => string.Concat(r.Elements<Text>().Select(t => t.Text))));

        int start = full.IndexOf(markerText, StringComparison.Ordinal);
        if (start < 0) return;

        string newFull = full.Remove(start, markerText.Length).TrimEnd();

        foreach (Run r in runs)
            r.Remove();

        if (newFull.Length > 0)
        {
            var run = new Run(new Text(newFull) { Space = SpaceProcessingModeValues.Preserve });
            paragraph.AppendChild(run);
        }
    }

    private static void InsertContentAfterParagraph(
        WordprocessingDocument document,
        Paragraph anchorParagraph,
        ContentBlock block,
        int parentHeadingLevel)
    {
        OpenXmlElement insertAfter = anchorParagraph;

        // Use the count of existing inline drawings to seed the unique ID counter so
        // multiple calls to this method within one document don't produce duplicate IDs.
        uint drawingId = (uint)(document.MainDocumentPart?.Document?.Body?
            .Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>().Count() ?? 0) + 1U;

        foreach (ContentItem item in block.Items)
        {
            IEnumerable<OpenXmlElement> elements = item switch
            {
                HeaderItem h => [CreateHeadingParagraph(h, parentHeadingLevel)],
                ParagraphItem p => [CreateBodyParagraph(p)],
                ImageItem img => CreateImageElements(document, img, ref drawingId),
                UnorderedListItem ul => CreateListParagraphs(ul.Items, listStyle: "ListBullet"),
                OrderedListItem ol => CreateListParagraphs(ol.Items, listStyle: "ListNumber"),
                TableItem table => CreateTableElements(table),
                _ => []
            };

            foreach (OpenXmlElement el in elements)
            {
                insertAfter.InsertAfterSelf(el);
                insertAfter = el;
            }
        }
    }

    private static Paragraph CreateHeadingParagraph(HeaderItem header, int parentLevel)
    {
        int actualLevel = Math.Clamp(parentLevel + header.Level, 1, 9);
        string styleId = $"Heading{actualLevel}";

        return new Paragraph(
            new ParagraphProperties(
                new ParagraphStyleId { Val = styleId }),
            new Run(
                new Text(header.Text) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private static Paragraph CreateBodyParagraph(ParagraphItem paragraph) =>
        new(new Run(new Text(paragraph.Text) { Space = SpaceProcessingModeValues.Preserve }));

    private static readonly HashSet<string> SupportedImageExtensions =
        ["jpg", "jpeg", "png", "gif", "bmp", "tiff", "tif"];

    private static List<OpenXmlElement> CreateImageElements(
        WordprocessingDocument document, ImageItem imageItem, ref uint drawingId)
    {
        if (!File.Exists(imageItem.Path))
        {
            return [CreateBodyParagraph(new ParagraphItem { Text = $"[Image not found: {imageItem.Path}]" })];
        }

        string extension = Path.GetExtension(imageItem.Path).TrimStart('.').ToLowerInvariant();
        if (!SupportedImageExtensions.Contains(extension))
            throw new ArgumentException(
                $"Unsupported image format '.{extension}'. Supported formats: jpg, jpeg, png, gif, bmp, tiff, tif.",
                nameof(imageItem));

        PartTypeInfo partType = extension switch
        {
            "png" => ImagePartType.Png,
            "gif" => ImagePartType.Gif,
            "bmp" => ImagePartType.Bmp,
            "tiff" or "tif" => ImagePartType.Tiff,
            _ => ImagePartType.Jpeg  // covers "jpg" and "jpeg"
        };

        MainDocumentPart mainPart = document.MainDocumentPart
            ?? throw new InvalidOperationException("No main document part.");

        ImagePart imagePart = mainPart.AddImagePart(partType);
        using (var stream = File.OpenRead(imageItem.Path))
            imagePart.FeedData(stream);

        string relationshipId = mainPart.GetIdOfPart(imagePart);
        const long defaultWidthEmu = 5_400_000L;
        const long defaultHeightEmu = 4_050_000L;
        uint currentId = drawingId++;

        var drawing = new Drawing(
            new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent
                {
                    Cx = defaultWidthEmu,
                    Cy = defaultHeightEmu
                },
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent
                {
                    LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L
                },
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties
                {
                    Id = currentId,
                    Name = Path.GetFileNameWithoutExtension(imageItem.Path)
                },
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties(
                    new DocumentFormat.OpenXml.Drawing.GraphicFrameLocks { NoChangeAspect = true }),
                new DocumentFormat.OpenXml.Drawing.Graphic(
                    new DocumentFormat.OpenXml.Drawing.GraphicData(
                        new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                            new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties
                                {
                                    Id = 0U,
                                    Name = Path.GetFileName(imageItem.Path)
                                },
                                new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()),
                            new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                                new DocumentFormat.OpenXml.Drawing.Blip { Embed = relationshipId },
                                new DocumentFormat.OpenXml.Drawing.Stretch(
                                    new DocumentFormat.OpenXml.Drawing.FillRectangle())),
                            new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                                new DocumentFormat.OpenXml.Drawing.Transform2D(
                                    new DocumentFormat.OpenXml.Drawing.Offset { X = 0L, Y = 0L },
                                    new DocumentFormat.OpenXml.Drawing.Extents
                                    {
                                        Cx = defaultWidthEmu,
                                        Cy = defaultHeightEmu
                                    }),
                                new DocumentFormat.OpenXml.Drawing.PresetGeometry(
                                    new DocumentFormat.OpenXml.Drawing.AdjustValueList())
                                {
                                    Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle
                                })))
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U
            });

        List<OpenXmlElement> result = [new Paragraph(new Run(drawing))];

        if (imageItem.Caption is not null)
        {
            result.Add(new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = "Caption" }),
                new Run(new Text(imageItem.Caption) { Space = SpaceProcessingModeValues.Preserve })));
        }

        return result;
    }

    private static IEnumerable<Paragraph> CreateListParagraphs(
        IReadOnlyList<string> items, string listStyle)
    {
        foreach (string item in items)
        {
            yield return new Paragraph(
                new ParagraphProperties(
                    new ParagraphStyleId { Val = listStyle }),
                new Run(
                    new Text(item) { Space = SpaceProcessingModeValues.Preserve }));
        }
    }

    private static IEnumerable<OpenXmlElement> CreateTableElements(TableItem tableItem)
    {
        var table = new Table();
        var tableProperties = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4U },
                new BottomBorder { Val = BorderValues.Single, Size = 4U },
                new LeftBorder { Val = BorderValues.Single, Size = 4U },
                new RightBorder { Val = BorderValues.Single, Size = 4U },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4U },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4U }));

        if (!string.IsNullOrWhiteSpace(tableItem.TableStyle))
        {
            tableProperties.PrependChild(new TableStyle { Val = tableItem.TableStyle });
        }

        table.AppendChild(tableProperties);

        if (tableItem.Header is not null)
        {
            table.AppendChild(CreateTableRow(tableItem.Header, isHeaderRow: true));
        }

        foreach (IReadOnlyList<string> row in tableItem.Rows)
        {
            table.AppendChild(CreateTableRow(row, isHeaderRow: false));
        }

        var elements = new List<OpenXmlElement> { table };

        if (tableItem.Caption is not null)
        {
            elements.Add(new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = "Caption" }),
                new Run(new Text(tableItem.Caption) { Space = SpaceProcessingModeValues.Preserve })));
        }

        return elements;
    }

    private static TableRow CreateTableRow(IReadOnlyList<string> cells, bool isHeaderRow)
    {
        var row = new TableRow();

        if (isHeaderRow)
        {
            row.AppendChild(new TableRowProperties(new TableHeader()));
        }

        foreach (string cellText in cells)
        {
            row.AppendChild(new TableCell(
                new Paragraph(
                    new Run(new Text(cellText) { Space = SpaceProcessingModeValues.Preserve })),
                new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Auto })));
        }

        return row;
    }
}
