namespace Binstore;
/// <summary>
/// Represents bin store result.
/// </summary>

public sealed record BinStoreResult(bool Created, long BinId, long TotalBytes, int ChunkCount);
