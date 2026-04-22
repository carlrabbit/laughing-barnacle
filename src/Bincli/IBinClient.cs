using Binstore;

namespace Bincli;

/// <summary>
/// Documentation.
/// </summary>
public interface IBinClient
{
    /// <summary>
    /// Documentation.
    /// </summary>
    Task<BinStoreResult> StoreAsync(string key, byte[] value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Documentation.
    /// </summary>
    Task<BinStoreResult> StoreAsync(string key, Stream valueStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Documentation.
    /// </summary>
    Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken = default);
}
