namespace Jsonstore;

public sealed class JsonChunkRecord
{
    public long Id { get; set; }
    public long JsonId { get; set; }
    public int ChunkIndex { get; set; }
    public required byte[] Data { get; set; }
}
