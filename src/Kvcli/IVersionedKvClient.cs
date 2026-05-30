using Kvstore;

namespace Kvcli;
/// <summary>
/// Defines the contract for i versioned kv client.
/// </summary>

public interface IVersionedKvClient
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

    Task<VersionedUpsertResult> UpsertBlobAsync(
        string key,
        string? expectedVersionId,
        Stream valueStream,
        CancellationToken cancellationToken = default);

    Task<KvReadResult?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task<Stream?> GetBlobStreamAsync(string key, CancellationToken cancellationToken = default);
}
