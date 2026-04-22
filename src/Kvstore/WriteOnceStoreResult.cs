namespace Kvstore;

/// <summary>
/// Represents the result of a write-once store attempt and its assigned version identifier.
/// </summary>
public sealed record WriteOnceStoreResult(bool Created, string VersionId);
