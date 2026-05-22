using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Blip = DocumentFormat.OpenXml.Drawing.Blip;
using Drawing = DocumentFormat.OpenXml.Wordprocessing.Drawing;

namespace MDE;

public sealed class MarkdownExtractor
{
    public void Extract(string inputFile, string outputFile, string imageDirectory, string mappingFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(imageDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(mappingFile);

        using var document = WordprocessingDocument.Open(inputFile, false);
        MainDocumentPart mainPart = document.MainDocumentPart
            ?? throw new InvalidOperationException("The document has no main part.");
        Body body = mainPart.Document.Body
            ?? throw new InvalidOperationException("The document has no body.");

        Dictionary<string, string> styleNamesById = BuildStyleNamesById(mainPart);
        SortedSet<string> unknownStyles = [];
        var markdown = new StringBuilder();
        int imageIndex = 0;

        foreach (OpenXmlElement element in body.ChildElements)
        {
            if (element is Paragraph paragraph)
            {
                ParagraphKind paragraphKind = ResolveParagraphKind(paragraph, styleNamesById, unknownStyles);
                string text = ExtractParagraphText(paragraph, mainPart, applyRunFormatting: paragraphKind.Type != ParagraphType.CodeBlock).Trim();

                if (!string.IsNullOrEmpty(text))
                {
                    markdown.AppendLine(FormatMarkdownLine(paragraphKind, text));
                    markdown.AppendLine();
                }

                AppendImageMarkdownLines(markdown, paragraph, mainPart, imageDirectory, outputFile, ref imageIndex);
            }
            else if (element is Table table)
            {
                foreach (string tableLine in ConvertTableToMarkdown(table, mainPart))
                {
                    markdown.AppendLine(tableLine);
                }

                markdown.AppendLine();
            }
        }

        EnsureParentDirectoryExists(outputFile);
        File.WriteAllText(outputFile, markdown.ToString(), Encoding.UTF8);
        WriteMappingFile(mappingFile, unknownStyles);
    }

    private static void EnsureParentDirectoryExists(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static Dictionary<string, string> BuildStyleNamesById(MainDocumentPart mainPart)
    {
        var styleNamesById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        StyleDefinitionsPart? styleDefinitionsPart = mainPart.StyleDefinitionsPart;
        if (styleDefinitionsPart?.Styles is null)
        {
            return styleNamesById;
        }

        foreach (Style style in styleDefinitionsPart.Styles.Elements<Style>())
        {
            string? styleId = style.StyleId?.Value;
            string? styleName = style.StyleName?.Val?.Value;
            if (string.IsNullOrWhiteSpace(styleId) || string.IsNullOrWhiteSpace(styleName))
            {
                continue;
            }

            styleNamesById[styleId] = styleName;
        }

        return styleNamesById;
    }

    private static ParagraphKind ResolveParagraphKind(
        Paragraph paragraph,
        IReadOnlyDictionary<string, string> styleNamesById,
        ISet<string> unknownStyles)
    {
        string? styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (string.IsNullOrWhiteSpace(styleId))
        {
            return ParagraphKind.Paragraph;
        }

        if (TryResolveKind(styleId, out ParagraphKind paragraphKind))
        {
            return paragraphKind;
        }

        styleNamesById.TryGetValue(styleId, out string? styleName);
        if (!string.IsNullOrWhiteSpace(styleName) && TryResolveKind(styleName, out paragraphKind))
        {
            return paragraphKind;
        }

        unknownStyles.Add(styleName ?? styleId);
        return ParagraphKind.Paragraph;
    }

    private static bool TryResolveKind(string style, out ParagraphKind paragraphKind)
    {
        string normalized = NormalizeStyle(style);

        paragraphKind = normalized switch
        {
            "heading1" or "h1" or "title" => ParagraphKind.Heading(1),
            "heading2" or "h2" => ParagraphKind.Heading(2),
            "heading3" or "h3" => ParagraphKind.Heading(3),
            "heading4" or "h4" => ParagraphKind.Heading(4),
            "heading5" or "h5" => ParagraphKind.Heading(5),
            "heading6" or "h6" => ParagraphKind.Heading(6),
            "listbullet" or "bullet" or "unorderedlist" => ParagraphKind.UnorderedList,
            "listnumber" or "numberedlist" or "orderedlist" => ParagraphKind.OrderedList,
            "quote" or "blockquote" => ParagraphKind.Quote,
            "code" or "codeblock" or "preformattedtext" => ParagraphKind.CodeBlock,
            _ => ParagraphKind.Unknown
        };

        if (paragraphKind == ParagraphKind.Unknown)
        {
            if (normalized.Contains("quote", StringComparison.Ordinal) ||
                normalized.Contains("blockquote", StringComparison.Ordinal) ||
                normalized.Contains("citation", StringComparison.Ordinal))
            {
                paragraphKind = ParagraphKind.Quote;
            }
            else if (normalized.Contains("code", StringComparison.Ordinal) ||
                     normalized.Contains("preformatted", StringComparison.Ordinal) ||
                     normalized.Contains("verbatim", StringComparison.Ordinal) ||
                     normalized.Contains("source", StringComparison.Ordinal))
            {
                paragraphKind = ParagraphKind.CodeBlock;
            }
        }

        return paragraphKind != ParagraphKind.Unknown;
    }

    private static string NormalizeStyle(string style)
    {
        var builder = new StringBuilder(style.Length);
        foreach (char character in style)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static string FormatMarkdownLine(ParagraphKind paragraphKind, string text)
    {
        return paragraphKind switch
        {
            { Type: ParagraphType.Heading } => $"{new string('#', paragraphKind.Level)} {text}",
            { Type: ParagraphType.UnorderedList } => $"- {text}",
            { Type: ParagraphType.OrderedList } => $"1. {text}",
            { Type: ParagraphType.Quote } => $"> {text}",
            { Type: ParagraphType.CodeBlock } => FormatCodeBlock(text),
            _ => text
        };
    }

    private static string ExtractParagraphText(Paragraph paragraph, MainDocumentPart mainPart, bool applyRunFormatting)
        => ExtractInlineText(paragraph.ChildElements, mainPart, applyRunFormatting);

    private static string ExtractInlineText(IEnumerable<OpenXmlElement> elements, MainDocumentPart mainPart, bool applyRunFormatting)
    {
        var textBuilder = new StringBuilder();

        foreach (OpenXmlElement child in elements)
        {
            switch (child)
            {
                case Run run:
                    textBuilder.Append(ExtractRunText(run, applyRunFormatting));
                    break;
                case Hyperlink hyperlink:
                    textBuilder.Append(ExtractHyperlinkText(hyperlink, mainPart, applyRunFormatting));
                    break;
            }
        }

        return textBuilder.ToString();
    }

    private static string ExtractRunText(Run run, bool applyFormatting)
    {
        string text = string.Concat(run.Descendants<Text>().Select(textElement => textElement.Text));
        if (!applyFormatting || string.IsNullOrEmpty(text))
        {
            return text;
        }

        RunProperties? properties = run.RunProperties;
        bool isBold = IsRunPropertyEnabled(properties?.Bold);
        bool isItalic = IsRunPropertyEnabled(properties?.Italic);
        bool isStrikethrough = IsRunPropertyEnabled(properties?.Strike);

        if (isBold)
        {
            text = $"**{text}**";
        }

        if (isItalic)
        {
            text = $"*{text}*";
        }

        if (isStrikethrough)
        {
            text = $"~~{text}~~";
        }

        return text;
    }

    private static bool IsRunPropertyEnabled(OnOffType? property) => property is { Val.Value: not false } or { Val: null };

    private static string ExtractHyperlinkText(Hyperlink hyperlink, MainDocumentPart mainPart, bool applyRunFormatting)
    {
        string text = ExtractInlineText(hyperlink.ChildElements, mainPart, applyRunFormatting).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string? url = ResolveHyperlinkUrl(hyperlink, mainPart);
        return string.IsNullOrWhiteSpace(url) ? text : $"[{text}]({url})";
    }

    private static string? ResolveHyperlinkUrl(Hyperlink hyperlink, MainDocumentPart mainPart)
    {
        string? relationshipId = hyperlink.Id?.Value;
        if (!string.IsNullOrWhiteSpace(relationshipId))
        {
            HyperlinkRelationship? relationship = mainPart.HyperlinkRelationships.FirstOrDefault(candidate =>
                candidate.Id.Equals(relationshipId, StringComparison.Ordinal));
            if (relationship is not null)
            {
                return relationship.Uri.ToString();
            }
        }

        string? anchor = hyperlink.Anchor?.Value;
        return string.IsNullOrWhiteSpace(anchor) ? null : $"#{anchor}";
    }

    private static IEnumerable<string> ConvertTableToMarkdown(Table table, MainDocumentPart mainPart)
    {
        List<IReadOnlyList<string>> rows = table.Elements<TableRow>()
            .Select(row => row.Elements<TableCell>()
                .Select(cell => EscapeTableCellText(ExtractTableCellText(cell, mainPart)))
                .ToList())
            .Cast<IReadOnlyList<string>>()
            .Where(row => row.Count > 0)
            .ToList();

        if (rows.Count == 0)
        {
            return [];
        }

        int columnCount = rows.Max(row => row.Count);
        List<IReadOnlyList<string>> paddedRows = rows
            .Select(row => PadCells(row, columnCount))
            .ToList();

        string header = $"| {string.Join(" | ", paddedRows[0])} |";
        string separator = $"| {string.Join(" | ", Enumerable.Repeat("---", columnCount))} |";

        var markdownRows = new List<string> { header, separator };
        markdownRows.AddRange(paddedRows.Skip(1).Select(row => $"| {string.Join(" | ", row)} |"));
        return markdownRows;
    }

    private static string ExtractTableCellText(TableCell cell, MainDocumentPart mainPart)
    {
        return string.Join(" ", cell.Elements<Paragraph>()
            .Select(paragraph => ExtractParagraphText(paragraph, mainPart, applyRunFormatting: true).Trim())
            .Where(text => !string.IsNullOrEmpty(text)));
    }

    private static IReadOnlyList<string> PadCells(IReadOnlyList<string> row, int columnCount)
    {
        var padded = row.ToList();
        while (padded.Count < columnCount)
        {
            padded.Add(string.Empty);
        }

        return padded;
    }

    private static string EscapeTableCellText(string text)
    {
        return text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static void AppendImageMarkdownLines(
        StringBuilder markdown,
        Paragraph paragraph,
        MainDocumentPart mainPart,
        string imageDirectory,
        string outputFile,
        ref int imageIndex)
    {
        foreach (Drawing drawing in paragraph.Descendants<Drawing>())
        {
            Blip? blip = drawing.Descendants<Blip>().FirstOrDefault();
            string? relationshipId = blip?.Embed?.Value;
            if (string.IsNullOrWhiteSpace(relationshipId))
            {
                continue;
            }

            if (mainPart.GetPartById(relationshipId) is not ImagePart imagePart)
            {
                continue;
            }

            string extension = GetImageExtension(imagePart.ContentType);
            imageIndex++;

            Directory.CreateDirectory(imageDirectory);
            string imageFileName = $"image{imageIndex}.{extension}";
            string imagePath = Path.Combine(imageDirectory, imageFileName);

            using Stream source = imagePart.GetStream();
            using var destination = File.Create(imagePath);
            source.CopyTo(destination);

            string outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputFile))
                ?? throw new InvalidOperationException("The output file directory could not be resolved.");
            string relativePath = Path.GetRelativePath(outputDirectory, imagePath).Replace('\\', '/');
            markdown.AppendLine($"![]({relativePath})");
            markdown.AppendLine();
        }
    }

    private static string GetImageExtension(string contentType)
    {
        return contentType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/gif" => "gif",
            "image/bmp" => "bmp",
            "image/tiff" => "tiff",
            _ => "bin"
        };
    }

    private static void WriteMappingFile(string mappingFile, IEnumerable<string> unknownStyles)
    {
        EnsureParentDirectoryExists(mappingFile);

        var mapping = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["_comment"] =
                "Markdown mapping targets: heading1, heading2, heading3, heading4, heading5, heading6, paragraph, unordered-list, ordered-list, blockquote, code-block, image, unknown"
        };

        foreach (string unknownStyle in unknownStyles)
        {
            mapping[unknownStyle] = "unknown";
        }

        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        File.WriteAllText(mappingFile, JsonSerializer.Serialize(mapping, serializerOptions), Encoding.UTF8);
    }

    private readonly record struct ParagraphKind(ParagraphType Type, int Level)
    {
        public static ParagraphKind Paragraph => new(ParagraphType.Paragraph, 0);
        public static ParagraphKind UnorderedList => new(ParagraphType.UnorderedList, 0);
        public static ParagraphKind OrderedList => new(ParagraphType.OrderedList, 0);
        public static ParagraphKind Quote => new(ParagraphType.Quote, 0);
        public static ParagraphKind CodeBlock => new(ParagraphType.CodeBlock, 0);
        public static ParagraphKind Unknown => new(ParagraphType.Unknown, 0);
        public static ParagraphKind Heading(int level) => new(ParagraphType.Heading, level);
    }

    private enum ParagraphType
    {
        Paragraph,
        Heading,
        UnorderedList,
        OrderedList,
        Quote,
        CodeBlock,
        Unknown
    }

    private static string FormatCodeBlock(string text)
    {
        string fence = text.Contains("```", StringComparison.Ordinal) ? "````" : "```";
        return $"{fence}\n{text}\n{fence}";
    }
}
