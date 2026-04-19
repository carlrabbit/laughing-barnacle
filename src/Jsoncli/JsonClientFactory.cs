using Microsoft.Extensions.DependencyInjection;

namespace Jsoncli;

public sealed class JsonClientFactory(IServiceProvider serviceProvider) : IJsonClientFactory
{
    public IJsonClient CreateClient() => serviceProvider.GetRequiredService<IJsonClient>();
}
