namespace Kvstore;

/// <summary>
/// Documentation.
/// </summary>
public interface IWriteOnceKeyValueStore
{
    /// <summary>
    /// Documentation.
    /// </summary>
    Task<WriteOnceStoreResult> StoreStringAsync(string key, string value, CancellationToken cancellationToken = default);
    /// <summary>
    /// Documentation.
    /// </summary>
    Task<WriteOnceStoreResult> StoreBlobAsync(string key, byte[] value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a blob by fully buffering <paramref name="valueStream"/> into memory before persistence.
    /// </summary>
    Task<WriteOnceStoreResult> StoreBlobAsync(string key, Stream valueStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Documentation.
    /// </summary>
    Task<KvReadResult?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the blob value as a new read-only in-memory stream. The returned stream is owned by the caller and must be disposed.
    /// </summary>
    Task<Stream?> GetBlobStreamAsync(string key, CancellationToken cancellationToken = default);
}
