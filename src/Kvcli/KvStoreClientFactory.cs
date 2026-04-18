namespace Kvcli;

public sealed class KvStoreClientFactory(
    IWriteOnceKvClient writeOnceClient,
    IVersionedKvClient versionedClient) : IKvStoreClientFactory
{
    public IWriteOnceKvClient CreateWriteOnceClient() => writeOnceClient;
    public IVersionedKvClient CreateVersionedClient() => versionedClient;
}
