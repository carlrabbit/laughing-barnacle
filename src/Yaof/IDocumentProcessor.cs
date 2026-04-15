using Yaof.Models;

namespace Yaof;

/// <summary>
/// Processes an Open XML .docx document by locating marker headings and inserting replacement content.
/// </summary>
public interface IDocumentProcessor
{
    /// <summary>
    /// Opens the document at <paramref name="docxPath"/>, applies all replacements defined in
    /// <paramref name="replacements"/>, and saves the modified document to <paramref name="outputPath"/>.
    /// </summary>
    void ProcessDocument(string docxPath, ReplacementDocument replacements, string outputPath);

    /// <summary>
    /// Opens the document stream, applies all replacements defined in
    /// <paramref name="replacements"/>, and writes the modified document to <paramref name="output"/>.
    /// </summary>
    void ProcessDocument(Stream docxStream, ReplacementDocument replacements, Stream output);
}
