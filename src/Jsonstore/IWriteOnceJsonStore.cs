using System.Text.Json;

namespace Jsonstore;
/// <summary>
/// Defines the contract for i write once json store.
/// </summary>

public interface IWriteOnceJsonStore
{
    Task<JsonStoreResult> StoreAsync(string key, string json, CancellationToken cancellationToken = default);

    Task<JsonStoreResult> StoreAsync(string key, Stream jsonStream, CancellationToken cancellationToken = default);

    Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken = default);

    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);

    Task<JsonRootType?> GetJsonTypeAsync(string key, CancellationToken cancellationToken = default);

    IAsyncEnumerable<JsonElement> GetObjectPropertiesAsync(string key, CancellationToken cancellationToken = default);

    IAsyncEnumerable<JsonElement> GetArrayElementsAsync(string key, CancellationToken cancellationToken = default);
}
