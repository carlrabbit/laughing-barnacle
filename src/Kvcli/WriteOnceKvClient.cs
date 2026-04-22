using Kvstore;

namespace Kvcli;
/// <summary>
/// Represents write once kv client.
/// </summary>

public sealed class WriteOnceKvClient(IWriteOnceKeyValueStore store) : IWriteOnceKvClient
{
    /// <summary>
    /// Performs the store string async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public Task<WriteOnceStoreResult> StoreStringAsync(
        string key,
        string value,
        CancellationToken cancellationToken = default) =>
        store.StoreStringAsync(key, value, cancellationToken);
    /// <summary>
    /// Performs the store blob async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public Task<WriteOnceStoreResult> StoreBlobAsync(
        string key,
        byte[] value,
        CancellationToken cancellationToken = default) =>
        store.StoreBlobAsync(key, value, cancellationToken);
    /// <summary>
    /// Performs the store blob async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="valueStream">The value stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public Task<WriteOnceStoreResult> StoreBlobAsync(
        string key,
        Stream valueStream,
        CancellationToken cancellationToken = default) =>
        store.StoreBlobAsync(key, valueStream, cancellationToken);
    /// <summary>
    /// Performs the get async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public Task<KvReadResult?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetAsync(key, cancellationToken);
    /// <summary>
    /// Performs the get blob stream async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public Task<Stream?> GetBlobStreamAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetBlobStreamAsync(key, cancellationToken);
}
