namespace LaughingBarnacle.Services;

/// <summary>
/// Stores JSON content and file metadata for the active editing session.
/// </summary>
public class JsonStorageService
{
    /// <summary>
    /// Gets or sets the current JSON content.
    /// </summary>
    public string? JsonContent { get; set; }

    /// <summary>
    /// Gets or sets the file name associated with <see cref="JsonContent"/>.
    /// </summary>
    public string? FileName { get; set; }
}
