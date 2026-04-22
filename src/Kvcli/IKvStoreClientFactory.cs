namespace Kvcli;

/// <summary>
/// Documentation.
/// </summary>
public interface IKvStoreClientFactory
{
    /// <summary>
    /// Documentation.
    /// </summary>
    IWriteOnceKvClient CreateWriteOnceClient();
    /// <summary>
    /// Documentation.
    /// </summary>
    IVersionedKvClient CreateVersionedClient();
}
