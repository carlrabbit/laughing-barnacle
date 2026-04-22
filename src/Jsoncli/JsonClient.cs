using System.Text.Json;
using Jsonstore;

namespace Jsoncli;

/// <summary>
/// Documentation.
/// </summary>
public sealed class JsonClient(IWriteOnceJsonStore store) : IJsonClient
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<JsonStoreResult> StoreAsync(string key, string json, CancellationToken cancellationToken = default) =>
        store.StoreAsync(key, json, cancellationToken);

    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<JsonStoreResult> StoreAsync(string key, Stream jsonStream, CancellationToken cancellationToken = default) =>
        store.StoreAsync(key, jsonStream, cancellationToken);

    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetStreamAsync(key, cancellationToken);

    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetStringAsync(key, cancellationToken);

    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<JsonRootType?> GetJsonTypeAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetJsonTypeAsync(key, cancellationToken);

    /// <summary>
    /// Documentation.
    /// </summary>
    public IAsyncEnumerable<JsonElement> GetObjectPropertiesAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetObjectPropertiesAsync(key, cancellationToken);

    /// <summary>
    /// Documentation.
    /// </summary>
    public IAsyncEnumerable<JsonElement> GetArrayElementsAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetArrayElementsAsync(key, cancellationToken);
}
