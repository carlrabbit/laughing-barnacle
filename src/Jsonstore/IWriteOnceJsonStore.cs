namespace Jsonstore;

public interface IWriteOnceJsonStore
{
    Task<JsonStoreResult> StoreAsync(string key, string json, CancellationToken cancellationToken = default);

    Task<JsonStoreResult> StoreAsync(string key, Stream jsonStream, CancellationToken cancellationToken = default);

    Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken = default);

    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);
}
