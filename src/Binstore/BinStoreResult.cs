namespace Binstore;

/// <summary>
/// Documentation.
/// </summary>
public sealed record BinStoreResult(bool Created, long BinId, long TotalBytes, int ChunkCount);
