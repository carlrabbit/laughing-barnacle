namespace Kvstore;

/// <summary>
/// Represents the outcome of a versioned upsert operation, including success, creation state, and resulting version.
/// </summary>
public sealed record VersionedUpsertResult(bool Success, bool Created, string? VersionId);
