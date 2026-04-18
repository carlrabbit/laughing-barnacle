namespace Kvstore;

public interface IWriteOnceKeyValueStore
{
    Task<WriteOnceStoreResult> StoreStringAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<WriteOnceStoreResult> StoreBlobAsync(string key, byte[] value, CancellationToken cancellationToken = default);
    Task<KvReadResult?> GetAsync(string key, CancellationToken cancellationToken = default);
}
