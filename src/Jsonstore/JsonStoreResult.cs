namespace Jsonstore;

public sealed record JsonStoreResult(bool Created, long JsonId, long TotalBytes, int ChunkCount);
