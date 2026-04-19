using System.Text;
using Jsoncli;
using Jsonstore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jsonstore.Tests;

public class JsonStoreBehaviorTests
{
    [Test]
    public async Task StoreAsync_WithLargeJsonStream_SplitsIntoChunksAndReadsBack()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceJsonStore(dbContext);
        var payload = BuildLargeJson(300_000);
        using var source = new MemoryStream(Encoding.UTF8.GetBytes(payload), writable: false);

        // Act
        var saved = await store.StoreAsync("json-key", source);
        using var retrieved = await store.GetStreamAsync("json-key");
        using var copied = new MemoryStream();
        await retrieved!.CopyToAsync(copied);

        // Assert
        await Assert.That(saved.Created).IsTrue();
        await Assert.That(saved.ChunkCount > 1).IsTrue();
        await Assert.That(Encoding.UTF8.GetString(copied.ToArray())).IsEqualTo(payload);
    }

    [Test]
    public async Task StoreAsync_WhenCalledTwiceOnSameKey_DoesNotOverwrite()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceJsonStore(dbContext);

        // Act
        var first = await store.StoreAsync("my-key", "{\"v\":1}");
        var second = await store.StoreAsync("my-key", "{\"v\":2}");
        var stored = await store.GetStringAsync("my-key");

        // Assert
        await Assert.That(first.Created).IsTrue();
        await Assert.That(second.Created).IsFalse();
        await Assert.That(stored).IsEqualTo("{\"v\":1}");
    }

    [Test]
    public async Task CreateClient_WithDependencyInjection_StoresAndFetchesValue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddJsonStore(options => options.UseInMemoryDatabase($"jsonstore-{Guid.NewGuid():N}"));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IJsonClientFactory>();
        var client = factory.CreateClient();

        // Act
        var created = await client.StoreAsync("di-key", "{\"ok\":true}");
        var fetched = await client.GetStringAsync("di-key");

        // Assert
        await Assert.That(created.Created).IsTrue();
        await Assert.That(fetched).IsEqualTo("{\"ok\":true}");
    }

    private static JsonStoreDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<JsonStoreDbContext>()
            .UseInMemoryDatabase($"jsonstore-tests-{Guid.NewGuid():N}")
            .Options;

        return new JsonStoreDbContext(options);
    }

    private static string BuildLargeJson(int valueCount)
    {
        var numbers = string.Join(',', Enumerable.Range(0, valueCount));
        return $"{{\"items\":[{numbers}]}}";
    }
}
