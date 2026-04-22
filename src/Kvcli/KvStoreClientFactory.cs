namespace Kvcli;
/// <summary>
/// Represents kv store client factory.
/// </summary>

public sealed class KvStoreClientFactory(
    IWriteOnceKvClient writeOnceClient,
    IVersionedKvClient versionedClient) : IKvStoreClientFactory
{
    /// <summary>
    /// Performs the create write once client operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public IWriteOnceKvClient CreateWriteOnceClient() => writeOnceClient;
    /// <summary>
    /// Performs the create versioned client operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public IVersionedKvClient CreateVersionedClient() => versionedClient;
}
