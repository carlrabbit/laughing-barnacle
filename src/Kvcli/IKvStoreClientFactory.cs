namespace Kvcli;

public interface IKvStoreClientFactory
{
    IWriteOnceKvClient CreateWriteOnceClient();
    IVersionedKvClient CreateVersionedClient();
}
