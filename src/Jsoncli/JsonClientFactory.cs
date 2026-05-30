using Microsoft.Extensions.DependencyInjection;

namespace Jsoncli;
/// <summary>
/// Represents json client factory.
/// </summary>

public sealed class JsonClientFactory(IServiceProvider serviceProvider) : IJsonClientFactory
{
    /// <summary>
    /// Performs the create client operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public IJsonClient CreateClient() => serviceProvider.GetRequiredService<IJsonClient>();
}
