using Microsoft.Extensions.DependencyInjection;

namespace Bincli;

public sealed class BinClientFactory(IServiceProvider serviceProvider) : IBinClientFactory
{
    public IBinClient CreateClient() => serviceProvider.GetRequiredService<IBinClient>();
}
