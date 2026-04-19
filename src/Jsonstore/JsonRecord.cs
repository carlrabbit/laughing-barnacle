namespace Jsonstore;

public sealed class JsonRecord
{
    public long Id { get; set; }
    public required string Key { get; set; }
    public required string KeyHash { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public long TotalBytes { get; set; }
    public int ChunkCount { get; set; }
}
