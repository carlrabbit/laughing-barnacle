namespace Kvstore;

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

    Task<KvReadResult?> GetAsync(string key, CancellationToken cancellationToken = default);
}
