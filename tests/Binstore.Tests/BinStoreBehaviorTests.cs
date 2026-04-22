using Bincli;
using Binstore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Binstore.Tests;

/// <summary>
/// Documentation.
/// </summary>
public class BinStoreBehaviorTests
{
    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task StoreAsync_WithLargeStream_SplitsIntoChunksAndReadsBack()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceBinaryStore(dbContext);
        var payload = Enumerable.Range(0, 300_000).Select(i => (byte)(i % 256)).ToArray();
        using var source = new MemoryStream(payload, writable: false);

        // Act
        var saved = await store.StoreAsync("blob-key", source);
        using var retrieved = await store.GetStreamAsync("blob-key");
        using var copied = new MemoryStream();
        await retrieved!.CopyToAsync(copied);

        // Assert
        await Assert.That(saved.Created).IsTrue();
        await Assert.That(saved.ChunkCount > 1).IsTrue();
        await Assert.That(copied.ToArray()).IsEquivalentTo(payload);
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task StoreAsync_WhenCalledTwiceOnSameKey_DoesNotOverwrite()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceBinaryStore(dbContext);

        // Act
        var first = await store.StoreAsync("my-key", [1, 2, 3]);
        var second = await store.StoreAsync("my-key", [9, 9, 9]);
        using var readStream = await store.GetStreamAsync("my-key");
        using var copied = new MemoryStream();
        await readStream!.CopyToAsync(copied);

        // Assert
        await Assert.That(first.Created).IsTrue();
        await Assert.That(second.Created).IsFalse();
        await Assert.That(Convert.ToBase64String(copied.ToArray()))
            .IsEqualTo(Convert.ToBase64String([1, 2, 3]));
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task CreateClient_WithDependencyInjection_StoresAndFetchesValue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBinStore(options => options.UseInMemoryDatabase($"binstore-{Guid.NewGuid():N}"));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IBinClientFactory>();
        var client = factory.CreateClient();

        // Act
        var created = await client.StoreAsync("di-key", [10, 20, 30]);
        using var stream = await client.GetStreamAsync("di-key");
        using var copied = new MemoryStream();
        await stream!.CopyToAsync(copied);

        // Assert
        await Assert.That(created.Created).IsTrue();
        await Assert.That(Convert.ToBase64String(copied.ToArray()))
            .IsEqualTo(Convert.ToBase64String([10, 20, 30]));
    }

    private static BinStoreDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BinStoreDbContext>()
            .UseInMemoryDatabase($"binstore-tests-{Guid.NewGuid():N}")
            .Options;

        return new BinStoreDbContext(options);
    }
}
