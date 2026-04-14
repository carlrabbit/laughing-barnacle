using System.Text.Json;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: JsonToMarkdown <path-to-json-file>");
    return 1;
}

var filePath = args[0];

if (!File.Exists(filePath))
{
    Console.Error.WriteLine($"File not found: {filePath}");
    return 1;
}

var json = await File.ReadAllTextAsync(filePath);
var tables = JsonToMarkdownConverter.Convert(json);

bool first = true;
foreach (var table in tables)
{
    if (!first)
        Console.WriteLine();

    Console.WriteLine(table);
    first = false;
}

return 0;

/// <summary>Converts a JSON array of flat string-property objects to Markdown tables.</summary>
public static class JsonToMarkdownConverter
{
    public static IEnumerable<string> Convert(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("JSON must be an array of objects.", nameof(json));

        foreach (var element in root.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
                continue;

            yield return BuildTable(element);
        }
    }

    private static string BuildTable(JsonElement obj)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("|Property|Description|");
        sb.AppendLine("|---|---|");

        foreach (var prop in obj.EnumerateObject())
        {
            var value = prop.Value.GetString() ?? string.Empty;
            sb.AppendLine($"|{prop.Name}|{value}|");
        }

        return sb.ToString().TrimEnd();
    }
}
