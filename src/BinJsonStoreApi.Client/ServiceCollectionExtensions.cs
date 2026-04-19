using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Polly;
using Polly.Extensions.Http;

namespace BinJsonStoreApi.Client;

public static class ServiceCollectionExtensions
{
    private const string HttpClientName = "BinJsonStoreApi.Client";
    private const int MaxRetryAttempts = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

    public static IHttpClientBuilder AddBinJsonStoreApiClient(
        this IServiceCollection services,
        string baseUrl)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        return services.AddBinJsonStoreApiClient(new Uri(baseUrl, UriKind.Absolute));
    }

    public static IHttpClientBuilder AddBinJsonStoreApiClient(
        this IServiceCollection services,
        Uri baseAddress)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(baseAddress);

        services.TryAddTransient<BinJsonStoreApiClient>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(HttpClientName);
            var requestAdapter = new HttpClientRequestAdapter(
                new AnonymousAuthenticationProvider(),
                httpClient: httpClient)
            {
                BaseUrl = baseAddress.ToString()
            };
            return new BinJsonStoreApiClient(requestAdapter);
        });

        return services
            .AddHttpClient(HttpClientName, client => client.BaseAddress = baseAddress)
            .AddPolicyHandler(CreateRetryPolicy());
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(MaxRetryAttempts, static _ => RetryDelay);
}
