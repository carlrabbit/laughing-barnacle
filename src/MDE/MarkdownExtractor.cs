using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
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

        foreach (Paragraph paragraph in body.Elements<Paragraph>())
        {
            ParagraphKind paragraphKind = ResolveParagraphKind(paragraph, styleNamesById, unknownStyles);
            string text = string.Concat(paragraph.Descendants<Text>().Select(textElement => textElement.Text)).Trim();

            if (!string.IsNullOrEmpty(text))
            {
                markdown.AppendLine(FormatMarkdownLine(paragraphKind, text));
                markdown.AppendLine();
            }

            AppendImageMarkdownLines(markdown, paragraph, mainPart, imageDirectory, outputFile, ref imageIndex);
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

        if (styleNamesById.TryGetValue(styleId, out string? styleName) && TryResolveKind(styleName, out paragraphKind))
        {
            return paragraphKind;
        }

        unknownStyles.Add(styleNamesById.TryGetValue(styleId, out string? mappedStyleName) ? mappedStyleName : styleId);
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
            _ => ParagraphKind.Unknown
        };

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
            _ => text
        };
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

            string outputDirectory = Path.GetDirectoryName(outputFile) ?? Directory.GetCurrentDirectory();
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
            ["_comment"] = "Markdown mapping targets: heading1, heading2, heading3, heading4, heading5, heading6, paragraph, unordered-list, ordered-list, image, unknown"
        };

        foreach (string unknownStyle in unknownStyles)
        {
            mapping[unknownStyle] = "unknown";
        }

        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        File.WriteAllText(mappingFile, JsonSerializer.Serialize(mapping, serializerOptions), Encoding.UTF8);
    }

    private readonly record struct ParagraphKind(ParagraphType Type, int Level)
    {
        public static ParagraphKind Paragraph => new(ParagraphType.Paragraph, 0);
        public static ParagraphKind UnorderedList => new(ParagraphType.UnorderedList, 0);
        public static ParagraphKind OrderedList => new(ParagraphType.OrderedList, 0);
        public static ParagraphKind Unknown => new(ParagraphType.Unknown, 0);
        public static ParagraphKind Heading(int level) => new(ParagraphType.Heading, level);
    }

    private enum ParagraphType
    {
        Paragraph,
        Heading,
        UnorderedList,
        OrderedList,
        Unknown
    }
}
