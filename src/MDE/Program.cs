using MDE;

if (!MarkdownExtractorCliArguments.TryParse(args, out MarkdownExtractorCliArguments? options, out string? errorMessage))
{
    Console.Error.WriteLine(errorMessage);
    Console.Error.WriteLine("Usage: MDE <input.docx> [output.md] [image-directory] [mapping-file]");
    return 1;
}

if (!File.Exists(options.InputFile))
{
    Console.Error.WriteLine($"Input file not found: {options.InputFile}");
    return 1;
}

var extractor = new MarkdownExtractor();
extractor.Extract(options.InputFile, options.OutputFile, options.ImageDirectory, options.MappingFile);
return 0;
