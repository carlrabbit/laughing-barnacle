namespace Jsonstore;

/// <summary>
/// Represents the result metadata for a JSON store operation.
/// </summary>
public sealed record JsonStoreResult(bool Created, long JsonId, JsonRootType JsonType, long TotalBytes, int ChunkCount);
