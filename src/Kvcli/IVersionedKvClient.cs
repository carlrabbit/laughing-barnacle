using Kvstore;

namespace Kvcli;

/// <summary>
/// Documentation.
/// </summary>
public interface IVersionedKvClient
{
    /// <summary>
    /// Documentation.
    /// </summary>
    Task<VersionedUpsertResult> UpsertStringAsync(
        string key,
        string? expectedVersionId,
        string value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Documentation.
    /// </summary>
    Task<VersionedUpsertResult> UpsertBlobAsync(
        string key,
        string? expectedVersionId,
        byte[] value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Documentation.
    /// </summary>
    Task<VersionedUpsertResult> UpsertBlobAsync(
        string key,
        string? expectedVersionId,
        Stream valueStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Documentation.
    /// </summary>
    Task<KvReadResult?> GetAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>
    /// Documentation.
    /// </summary>
    Task<Stream?> GetBlobStreamAsync(string key, CancellationToken cancellationToken = default);
}
