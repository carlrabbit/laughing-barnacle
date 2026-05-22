namespace MDE;

public sealed record MarkdownExtractorCliArguments(
    string InputFile,
    string OutputFile,
    string ImageDirectory,
    string MappingFile)
{
    public static bool TryParse(
        IReadOnlyList<string> args,
        out MarkdownExtractorCliArguments? options,
        out string? errorMessage)
    {
        if (args.Count is < 1 or > 4)
        {
            options = null;
            errorMessage = "Expected between 1 and 4 arguments.";
            return false;
        }

        string inputFile = Path.GetFullPath(args[0]);
        string outputFile = args.Count > 1
            ? Path.GetFullPath(args[1])
            : Path.ChangeExtension(inputFile, ".md");

        string inputStem = Path.Combine(
            Path.GetDirectoryName(inputFile) ?? string.Empty,
            Path.GetFileNameWithoutExtension(inputFile));

        string imageDirectory = args.Count > 2
            ? Path.GetFullPath(args[2])
            : $"{inputStem}_imaged";

        string mappingFile = args.Count > 3
            ? Path.GetFullPath(args[3])
            : $"{inputFile}.mapping.json";

        options = new MarkdownExtractorCliArguments(inputFile, outputFile, imageDirectory, mappingFile);
        errorMessage = null;
        return true;
    }
}
