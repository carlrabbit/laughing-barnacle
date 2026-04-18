namespace Kvstore;

public sealed record VersionedUpsertResult(bool Success, bool Created, string? VersionId);
