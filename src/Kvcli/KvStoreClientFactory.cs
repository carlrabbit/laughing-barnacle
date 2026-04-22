namespace Kvcli;

/// <summary>
/// Documentation.
/// </summary>
public sealed class KvStoreClientFactory(
    IWriteOnceKvClient writeOnceClient,
    IVersionedKvClient versionedClient) : IKvStoreClientFactory
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public IWriteOnceKvClient CreateWriteOnceClient() => writeOnceClient;
    /// <summary>
    /// Documentation.
    /// </summary>
    public IVersionedKvClient CreateVersionedClient() => versionedClient;
}
