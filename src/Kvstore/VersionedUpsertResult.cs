namespace Kvstore;
/// <summary>
/// Represents versioned upsert result.
/// </summary>

public sealed record VersionedUpsertResult(bool Success, bool Created, string? VersionId);
