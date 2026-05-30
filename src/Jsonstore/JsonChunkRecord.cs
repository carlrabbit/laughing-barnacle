namespace Jsonstore;
/// <summary>
/// Represents json chunk record.
/// </summary>

public sealed class JsonChunkRecord
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Gets or sets the json id.
    /// </summary>
    public long JsonId { get; set; }
    /// <summary>
    /// Gets or sets the chunk index.
    /// </summary>
    public int ChunkIndex { get; set; }
    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    public required byte[] Data { get; set; }
}
