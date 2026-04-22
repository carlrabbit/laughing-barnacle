using Kvstore;

namespace Kvcli;

/// <summary>
/// Documentation.
/// </summary>
public interface IWriteOnceKvClient
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
    /// Documentation.
    /// </summary>
    Task<WriteOnceStoreResult> StoreBlobAsync(string key, Stream valueStream, CancellationToken cancellationToken = default);
    /// <summary>
    /// Documentation.
    /// </summary>
    Task<KvReadResult?> GetAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>
    /// Documentation.
    /// </summary>
    Task<Stream?> GetBlobStreamAsync(string key, CancellationToken cancellationToken = default);
}
