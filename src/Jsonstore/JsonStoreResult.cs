namespace Jsonstore;
/// <summary>
/// Represents json store result.
/// </summary>

public sealed record JsonStoreResult(bool Created, long JsonId, JsonRootType JsonType, long TotalBytes, int ChunkCount);
