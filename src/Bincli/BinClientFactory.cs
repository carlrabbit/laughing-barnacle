using Microsoft.Extensions.DependencyInjection;

namespace Bincli;
/// <summary>
/// Represents bin client factory.
/// </summary>

public sealed class BinClientFactory(IServiceProvider serviceProvider) : IBinClientFactory
{
    /// <summary>
    /// Performs the create client operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public IBinClient CreateClient() => serviceProvider.GetRequiredService<IBinClient>();
}
