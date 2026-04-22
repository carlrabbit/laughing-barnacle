using Binstore;

namespace Bincli;

/// <summary>
/// Documentation.
/// </summary>
public sealed class BinClient(IWriteOnceBinaryStore store) : IBinClient
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<BinStoreResult> StoreAsync(string key, byte[] value, CancellationToken cancellationToken = default) =>
        store.StoreAsync(key, value, cancellationToken);

    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<BinStoreResult> StoreAsync(string key, Stream valueStream, CancellationToken cancellationToken = default) =>
        store.StoreAsync(key, valueStream, cancellationToken);

    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetStreamAsync(key, cancellationToken);
}
