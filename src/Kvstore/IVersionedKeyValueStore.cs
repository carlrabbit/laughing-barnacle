namespace Kvstore;
/// <summary>
/// Defines the contract for i versioned key value store.
/// </summary>

public interface IVersionedKeyValueStore
{
    Task<VersionedUpsertResult> UpsertStringAsync(
        string key,
        string? expectedVersionId,
        string value,
        CancellationToken cancellationToken = default);

    Task<VersionedUpsertResult> UpsertBlobAsync(
        string key,
        string? expectedVersionId,
        byte[] value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a blob by fully buffering <paramref name="valueStream"/> into memory before persistence.
    /// </summary>
    Task<VersionedUpsertResult> UpsertBlobAsync(
        string key,
        string? expectedVersionId,
        Stream valueStream,
        CancellationToken cancellationToken = default);

    Task<KvReadResult?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the blob value as a new read-only in-memory stream. The returned stream is owned by the caller and must be disposed.
    /// </summary>
    Task<Stream?> GetBlobStreamAsync(string key, CancellationToken cancellationToken = default);
}
