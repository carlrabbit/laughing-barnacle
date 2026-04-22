namespace Binstore;
/// <summary>
/// Defines the contract for i write once binary store.
/// </summary>

public interface IWriteOnceBinaryStore
{
    Task<BinStoreResult> StoreAsync(string key, byte[] value, CancellationToken cancellationToken = default);

    Task<BinStoreResult> StoreAsync(string key, Stream valueStream, CancellationToken cancellationToken = default);

    Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken = default);
}
