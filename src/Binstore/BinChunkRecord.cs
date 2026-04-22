namespace Binstore;

/// <summary>
/// Documentation.
/// </summary>
public sealed class BinChunkRecord
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public long BinId { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public int ChunkIndex { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public required byte[] Data { get; set; }
}
