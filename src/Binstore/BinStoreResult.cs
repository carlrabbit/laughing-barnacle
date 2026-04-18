namespace Binstore;

public sealed record BinStoreResult(bool Created, long BinId, long TotalBytes, int ChunkCount);
