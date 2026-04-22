using Kvstore;

namespace Kvcli;

/// <summary>
/// Documentation.
/// </summary>
public sealed class VersionedKvClient(IVersionedKeyValueStore store) : IVersionedKvClient
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<VersionedUpsertResult> UpsertStringAsync(
        string key,
        string? expectedVersionId,
        string value,
        CancellationToken cancellationToken = default) =>
        store.UpsertStringAsync(key, expectedVersionId, value, cancellationToken);

    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<VersionedUpsertResult> UpsertBlobAsync(
        string key,
        string? expectedVersionId,
        byte[] value,
        CancellationToken cancellationToken = default) =>
        store.UpsertBlobAsync(key, expectedVersionId, value, cancellationToken);

    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<VersionedUpsertResult> UpsertBlobAsync(
        string key,
        string? expectedVersionId,
        Stream valueStream,
        CancellationToken cancellationToken = default) =>
        store.UpsertBlobAsync(key, expectedVersionId, valueStream, cancellationToken);

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
