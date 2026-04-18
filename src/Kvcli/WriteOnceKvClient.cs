using Kvstore;

namespace Kvcli;

public sealed class WriteOnceKvClient(IWriteOnceKeyValueStore store) : IWriteOnceKvClient
{
    public Task<WriteOnceStoreResult> StoreStringAsync(
        string key,
        string value,
        CancellationToken cancellationToken = default) =>
        store.StoreStringAsync(key, value, cancellationToken);

    public Task<WriteOnceStoreResult> StoreBlobAsync(
        string key,
        byte[] value,
        CancellationToken cancellationToken = default) =>
        store.StoreBlobAsync(key, value, cancellationToken);

    public Task<KvReadResult?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetAsync(key, cancellationToken);
}
