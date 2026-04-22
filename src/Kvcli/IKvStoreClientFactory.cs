namespace Kvcli;
/// <summary>
/// Defines the contract for i kv store client factory.
/// </summary>

public interface IKvStoreClientFactory
{
    IWriteOnceKvClient CreateWriteOnceClient();
    IVersionedKvClient CreateVersionedClient();
}
