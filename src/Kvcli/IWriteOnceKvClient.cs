using Kvstore;

namespace Kvcli;
/// <summary>
/// Defines the contract for i write once kv client.
/// </summary>

public interface IWriteOnceKvClient
{
    Task<WriteOnceStoreResult> StoreStringAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<WriteOnceStoreResult> StoreBlobAsync(string key, byte[] value, CancellationToken cancellationToken = default);
    Task<WriteOnceStoreResult> StoreBlobAsync(string key, Stream valueStream, CancellationToken cancellationToken = default);
    Task<KvReadResult?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task<Stream?> GetBlobStreamAsync(string key, CancellationToken cancellationToken = default);
}
