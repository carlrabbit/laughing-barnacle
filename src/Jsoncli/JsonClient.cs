using System.Text.Json;
using Jsonstore;

namespace Jsoncli;
/// <summary>
/// Represents json client.
/// </summary>

public sealed class JsonClient(IWriteOnceJsonStore store) : IJsonClient
{
    /// <summary>
    /// Performs the store async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="json">The json.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public Task<JsonStoreResult> StoreAsync(string key, string json, CancellationToken cancellationToken = default) =>
        store.StoreAsync(key, json, cancellationToken);
    /// <summary>
    /// Performs the store async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="jsonStream">The json stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public Task<JsonStoreResult> StoreAsync(string key, Stream jsonStream, CancellationToken cancellationToken = default) =>
        store.StoreAsync(key, jsonStream, cancellationToken);
    /// <summary>
    /// Performs the get stream async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetStreamAsync(key, cancellationToken);
    /// <summary>
    /// Performs the get string async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetStringAsync(key, cancellationToken);
    /// <summary>
    /// Performs the get json type async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public Task<JsonRootType?> GetJsonTypeAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetJsonTypeAsync(key, cancellationToken);
    /// <summary>
    /// Performs the get object properties async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public IAsyncEnumerable<JsonElement> GetObjectPropertiesAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetObjectPropertiesAsync(key, cancellationToken);
    /// <summary>
    /// Performs the get array elements async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public IAsyncEnumerable<JsonElement> GetArrayElementsAsync(string key, CancellationToken cancellationToken = default) =>
        store.GetArrayElementsAsync(key, cancellationToken);
}
