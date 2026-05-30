namespace Binstore;
/// <summary>
/// Represents bin chunk record.
/// </summary>

public sealed class BinChunkRecord
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Gets or sets the bin id.
    /// </summary>
    public long BinId { get; set; }
    /// <summary>
    /// Gets or sets the chunk index.
    /// </summary>
    public int ChunkIndex { get; set; }
    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    public required byte[] Data { get; set; }
}
