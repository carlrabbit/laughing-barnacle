namespace Kvstore;
/// <summary>
/// Represents write once store result.
/// </summary>

public sealed record WriteOnceStoreResult(bool Created, string VersionId);
