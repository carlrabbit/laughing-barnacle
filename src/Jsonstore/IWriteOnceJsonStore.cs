using System.Text.Json;

namespace Jsonstore;

/// <summary>
/// Documentation.
/// </summary>
public interface IWriteOnceJsonStore
{
    /// <summary>
    /// Documentation.
    /// </summary>
    Task<JsonStoreResult> StoreAsync(string key, string json, CancellationToken cancellationToken = default);

    /// <summary>
    /// Documentation.
    /// </summary>
    Task<JsonStoreResult> StoreAsync(string key, Stream jsonStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Documentation.
    /// </summary>
    Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Documentation.
    /// </summary>
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Documentation.
    /// </summary>
    Task<JsonRootType?> GetJsonTypeAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Documentation.
    /// </summary>
    IAsyncEnumerable<JsonElement> GetObjectPropertiesAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Documentation.
    /// </summary>
    IAsyncEnumerable<JsonElement> GetArrayElementsAsync(string key, CancellationToken cancellationToken = default);
}
