using Microsoft.Extensions.DependencyInjection;

namespace Jsoncli;

/// <summary>
/// Documentation.
/// </summary>
public sealed class JsonClientFactory(IServiceProvider serviceProvider) : IJsonClientFactory
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public IJsonClient CreateClient() => serviceProvider.GetRequiredService<IJsonClient>();
}
