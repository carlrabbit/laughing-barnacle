namespace Jsonstore;

/// <summary>
/// Documentation.
/// </summary>
public sealed class JsonChunkRecord
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public long JsonId { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public int ChunkIndex { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public required byte[] Data { get; set; }
}
