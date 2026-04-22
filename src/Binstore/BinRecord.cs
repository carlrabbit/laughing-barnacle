namespace Binstore;

/// <summary>
/// Documentation.
/// </summary>
public sealed class BinRecord
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public required string Key { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public required string KeyHash { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public long TotalBytes { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public int ChunkCount { get; set; }
}
