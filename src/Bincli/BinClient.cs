using Binstore;

namespace Bincli;

public sealed class BinClient(IWriteOnceBinaryStore store) : IBinClient
{
    public Task<BinStoreResult> StoreAsync(string key, byte[] value, CancellationToken cancellationToken = default) =>
        store.StoreAsync(key, value, cancellationToken);

    public Task<BinStoreResult> StoreAsync(string key, Stream valueStream, CancellationToken cancellationToken = default) =>
        store.StoreAsync(key, valueStream, cancellationToken);

    public Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetStreamAsync(key, cancellationToken);
}
