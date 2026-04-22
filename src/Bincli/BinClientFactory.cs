using Microsoft.Extensions.DependencyInjection;

namespace Bincli;

/// <summary>
/// Documentation.
/// </summary>
public sealed class BinClientFactory(IServiceProvider serviceProvider) : IBinClientFactory
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public IBinClient CreateClient() => serviceProvider.GetRequiredService<IBinClient>();
}
