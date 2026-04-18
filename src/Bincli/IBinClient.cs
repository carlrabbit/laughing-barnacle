using Binstore;

namespace Bincli;

public interface IBinClient
{
    Task<BinStoreResult> StoreAsync(string key, byte[] value, CancellationToken cancellationToken = default);

    Task<BinStoreResult> StoreAsync(string key, Stream valueStream, CancellationToken cancellationToken = default);

    Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken = default);
}
