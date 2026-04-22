namespace Jsoncli;
/// <summary>
/// Defines the contract for i json client factory.
/// </summary>

public interface IJsonClientFactory
{
    IJsonClient CreateClient();
}
