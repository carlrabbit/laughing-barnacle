namespace Jsonstore;

public sealed record JsonStoreResult(bool Created, long JsonId, JsonRootType JsonType, long TotalBytes, int ChunkCount);
