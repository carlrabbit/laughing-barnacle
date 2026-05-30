namespace Jsonstore;
/// <summary>
/// Represents json record.
/// </summary>

public sealed class JsonRecord
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    public required string Key { get; set; }
    /// <summary>
    /// Gets or sets the key hash.
    /// </summary>
    public required string KeyHash { get; set; }
    /// <summary>
    /// Gets or sets the json type.
    /// </summary>
    public JsonRootType JsonType { get; set; }
    /// <summary>
    /// Gets or sets the created utc.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; }
    /// <summary>
    /// Gets or sets the total bytes.
    /// </summary>
    public long TotalBytes { get; set; }
    /// <summary>
    /// Gets or sets the chunk count.
    /// </summary>
    public int ChunkCount { get; set; }
}
