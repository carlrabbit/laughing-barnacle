namespace Binstore;

public sealed class BinChunkRecord
{
    public long Id { get; set; }
    public long BinId { get; set; }
    public int ChunkIndex { get; set; }
    public required byte[] Data { get; set; }
}
