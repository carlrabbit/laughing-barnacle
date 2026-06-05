namespace LaughingBarnacle.Services;

/// <summary>
/// Stores uploaded JSON content and metadata for the current user session.
/// </summary>
public class JsonStorageService
{
    /// <summary>
    /// Gets or sets the raw JSON content provided by the user.
    /// </summary>
    public string? JsonContent { get; set; }

    /// <summary>
    /// Gets or sets the name of the uploaded file associated with <see cref="JsonContent"/>.
    /// </summary>
    public string? FileName { get; set; }
}
