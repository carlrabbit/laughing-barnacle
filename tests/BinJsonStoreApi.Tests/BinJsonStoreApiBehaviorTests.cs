using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BinJsonStoreApi.Tests;

/// <summary>
/// Documentation.
/// </summary>
public class BinJsonStoreApiBehaviorTests
{
    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task PostAndGetBin_WithBinaryPayload_RoundTripsStreamedBytes()
    {
        // Arrange
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var payload = Enumerable.Range(0, 2048).Select(static i => (byte)(i % 256)).ToArray();

        // Act
        using var postContent = new StreamContent(new MemoryStream(payload, writable: false));
        postContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        using var postResponse = await client.PostAsync("/bin/blob-key", postContent);
        using var getResponse = await client.GetAsync("/bin/blob-key");
        var roundTripped = await getResponse.Content.ReadAsByteArrayAsync();

        // Assert
        await Assert.That(postResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(roundTripped).IsEquivalentTo(payload);
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task ObjectPropertiesStream_WithStoredObject_ReturnsNdjsonValues()
    {
        // Arrange
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var postContent = new StringContent("{\"a\":1,\"b\":{\"nested\":true}}", Encoding.UTF8, "application/json");
        using var postResponse = await client.PostAsync("/json/object-key", postContent);

        // Act
        using var streamResponse = await client.GetAsync("/json/object-key/object-properties-stream");
        var body = await streamResponse.Content.ReadAsStringAsync();
        var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Assert
        await Assert.That(postResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(streamResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(streamResponse.Content.Headers.ContentType?.MediaType).IsEqualTo("application/x-ndjson");
        await Assert.That(lines).IsEquivalentTo(["1", "{\"nested\":true}"]);
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task ArrayElementsStream_WithStoredArray_ReturnsNdjsonValues()
    {
        // Arrange
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var postContent = new StringContent("[1,{\"id\":2},true]", Encoding.UTF8, "application/json");
        using var postResponse = await client.PostAsync("/json/array-key", postContent);

        // Act
        using var streamResponse = await client.GetAsync("/json/array-key/array-elements-stream");
        var body = await streamResponse.Content.ReadAsStringAsync();
        var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Assert
        await Assert.That(postResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(streamResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(streamResponse.Content.Headers.ContentType?.MediaType).IsEqualTo("application/x-ndjson");
        await Assert.That(lines).IsEquivalentTo(["1", "{\"id\":2}", "true"]);
    }
}
