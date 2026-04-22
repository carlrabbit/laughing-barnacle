using System.Diagnostics;
using System.Net;
using System.Text;
using BinJsonStoreApi.Client;
using Microsoft.Extensions.DependencyInjection;

namespace BinJsonStoreApi.Tests;

/// <summary>
/// Documentation.
/// </summary>
public class BinJsonStoreApiClientRetryBehaviorTests
{
    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task AddBinJsonStoreApiClient_WhenTransientFailuresOccur_RetriesFiveTimesWithDelay()
    {
        // Arrange
        var handler = new SequenceHttpMessageHandler(
            HttpStatusCode.InternalServerError,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.OK);

        var services = new ServiceCollection();
        services.AddBinJsonStoreApiClient(new Uri("http://localhost"))
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<BinJsonStoreApiClient>();

        // Act
        var stopwatch = Stopwatch.StartNew();
        await using var result = await client.Json["retry-key"].GetAsync();
        stopwatch.Stop();

        // Assert
        await Assert.That(handler.AttemptCount).IsEqualTo(6);
        await Assert.That(stopwatch.Elapsed).IsGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(500));
        await Assert.That(result).IsNotNull();
    }

    private sealed class SequenceHttpMessageHandler(params HttpStatusCode[] responses) : HttpMessageHandler
    {
        private readonly HttpStatusCode[] _responses = responses;
        private int _attemptCount;

        public int AttemptCount => _attemptCount;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var attempt = Interlocked.Increment(ref _attemptCount);
            var statusCode = attempt <= _responses.Length
                ? _responses[attempt - 1]
                : _responses[^1];
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("{}"), writable: false))
            };
            return Task.FromResult(response);
        }
    }
}
