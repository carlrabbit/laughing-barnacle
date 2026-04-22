using Kvstore;

namespace Kvcli;

/// <summary>
/// Documentation.
/// </summary>
public sealed class WriteOnceKvClient(IWriteOnceKeyValueStore store) : IWriteOnceKvClient
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<WriteOnceStoreResult> StoreStringAsync(
        string key,
        string value,
        CancellationToken cancellationToken = default) =>
        store.StoreStringAsync(key, value, cancellationToken);

    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<WriteOnceStoreResult> StoreBlobAsync(
        string key,
        byte[] value,
        CancellationToken cancellationToken = default) =>
        store.StoreBlobAsync(key, value, cancellationToken);

    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<WriteOnceStoreResult> StoreBlobAsync(
        string key,
        Stream valueStream,
        CancellationToken cancellationToken = default) =>
        store.StoreBlobAsync(key, valueStream, cancellationToken);

    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<KvReadResult?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetAsync(key, cancellationToken);

    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<Stream?> GetBlobStreamAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetBlobStreamAsync(key, cancellationToken);
}
