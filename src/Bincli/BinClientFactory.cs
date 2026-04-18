namespace Bincli;

public sealed class BinClientFactory(IBinClient client) : IBinClientFactory
{
    public IBinClient CreateClient() => client;
}
