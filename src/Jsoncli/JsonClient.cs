using System.Text.Json;
using Jsonstore;

namespace Jsoncli;

public sealed class JsonClient(IWriteOnceJsonStore store) : IJsonClient
{
    public Task<JsonStoreResult> StoreAsync(string key, string json, CancellationToken cancellationToken = default) =>
        store.StoreAsync(key, json, cancellationToken);

    public Task<JsonStoreResult> StoreAsync(string key, Stream jsonStream, CancellationToken cancellationToken = default) =>
        store.StoreAsync(key, jsonStream, cancellationToken);

    public Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetStreamAsync(key, cancellationToken);

    public Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetStringAsync(key, cancellationToken);

    public Task<JsonRootType?> GetJsonTypeAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetJsonTypeAsync(key, cancellationToken);

    public IAsyncEnumerable<JsonElement> GetObjectPropertiesAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetObjectPropertiesAsync(key, cancellationToken);

    public IAsyncEnumerable<JsonElement> GetArrayElementsAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetArrayElementsAsync(key, cancellationToken);
}
