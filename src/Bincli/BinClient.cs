using Binstore;

namespace Bincli;
/// <summary>
/// Represents bin client.
/// </summary>

public sealed class BinClient(IWriteOnceBinaryStore store) : IBinClient
{
    /// <summary>
    /// Performs the store async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public Task<BinStoreResult> StoreAsync(string key, byte[] value, CancellationToken cancellationToken = default) =>
        store.StoreAsync(key, value, cancellationToken);
    /// <summary>
    /// Performs the store async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="valueStream">The value stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public Task<BinStoreResult> StoreAsync(string key, Stream valueStream, CancellationToken cancellationToken = default) =>
        store.StoreAsync(key, valueStream, cancellationToken);
    /// <summary>
    /// Performs the get stream async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetStreamAsync(key, cancellationToken);
}
