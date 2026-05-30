using Kvstore;

namespace Kvcli;
/// <summary>
/// Represents versioned kv client.
/// </summary>

public sealed class VersionedKvClient(IVersionedKeyValueStore store) : IVersionedKvClient
{
    /// <summary>
    /// Performs the upsert string async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="expectedVersionId">The expected version id.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public Task<VersionedUpsertResult> UpsertStringAsync(
        string key,
        string? expectedVersionId,
        string value,
        CancellationToken cancellationToken = default) =>
        store.UpsertStringAsync(key, expectedVersionId, value, cancellationToken);
    /// <summary>
    /// Performs the upsert blob async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="expectedVersionId">The expected version id.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public Task<VersionedUpsertResult> UpsertBlobAsync(
        string key,
        string? expectedVersionId,
        byte[] value,
        CancellationToken cancellationToken = default) =>
        store.UpsertBlobAsync(key, expectedVersionId, value, cancellationToken);
    /// <summary>
    /// Performs the upsert blob async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="expectedVersionId">The expected version id.</param>
    /// <param name="valueStream">The value stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public Task<VersionedUpsertResult> UpsertBlobAsync(
        string key,
        string? expectedVersionId,
        Stream valueStream,
        CancellationToken cancellationToken = default) =>
        store.UpsertBlobAsync(key, expectedVersionId, valueStream, cancellationToken);
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
