using Kvcli;
using Kvstore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kvstore.Tests;

public class KvStoreBehaviorTests
{
    [Test]
    public async Task StoreStringAsync_WhenCalledTwiceOnWriteOnceStore_PersistsFirstValueOnly()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceKeyValueStore(dbContext);

        // Act
        var first = await store.StoreStringAsync("my-key", "first");
        var second = await store.StoreStringAsync("my-key", "second");
        var read = await store.GetAsync("my-key");

        // Assert
        await Assert.That(first.Created).IsTrue();
        await Assert.That(second.Created).IsFalse();
        await Assert.That(read).IsNotNull();
        await Assert.That(read!.AsString()).IsEqualTo("first");
    }

    [Test]
    public async Task UpsertStringAsync_WithOutdatedVersion_RejectsUpdate()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new VersionedKeyValueStore(dbContext);
        var created = await store.UpsertStringAsync("versioned-key", null, "v1");
        _ = await store.UpsertStringAsync("versioned-key", created.VersionId, "v2");

        // Act
        var rejected = await store.UpsertStringAsync("versioned-key", created.VersionId, "v3");
        var read = await store.GetAsync("versioned-key");

        // Assert
        await Assert.That(rejected.Success).IsFalse();
        await Assert.That(read).IsNotNull();
        await Assert.That(read!.AsString()).IsEqualTo("v2");
    }

    [Test]
    public async Task UpsertStringAsync_WhenCreateUsesNonEmptyVersion_RejectsCreate()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new VersionedKeyValueStore(dbContext);

        // Act
        var result = await store.UpsertStringAsync("new-key", "some-version", "value");

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Created).IsFalse();
    }

    [Test]
    public async Task StoreStringAsync_WhenKeyIsTooLarge_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceKeyValueStore(dbContext);
        var oversizedKey = new string('a', KvStoreDbContext.MaxKeyBytes + 1);

        // Act / Assert
        await Assert.That(async () => await store.StoreStringAsync(oversizedKey, "value"))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task CreateWriteOnceClient_WithDependencyInjection_StoresAndFetchesValue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKvStore(options => options.UseInMemoryDatabase($"kvstore-{Guid.NewGuid():N}"));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IKvStoreClientFactory>();
        var client = factory.CreateWriteOnceClient();

        // Act
        var created = await client.StoreStringAsync("di-key", "di-value");
        var read = await client.GetAsync("di-key");

        // Assert
        await Assert.That(created.Created).IsTrue();
        await Assert.That(read).IsNotNull();
        await Assert.That(read!.AsString()).IsEqualTo("di-value");
    }

    [Test]
    public async Task StoreBlobAsync_WithStream_PersistsAndReturnsBlobStream()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceKeyValueStore(dbContext);
        using var source = new MemoryStream([1, 2, 3, 4, 5]);

        // Act
        var saved = await store.StoreBlobAsync("blob-key", source);
        using var retrieved = await store.GetBlobStreamAsync("blob-key");
        using var copied = new MemoryStream();
        await retrieved!.CopyToAsync(copied);

        // Assert
        await Assert.That(saved.Created).IsTrue();
        await Assert.That(Convert.ToBase64String(copied.ToArray()))
            .IsEqualTo(Convert.ToBase64String([1, 2, 3, 4, 5]));
    }

    [Test]
    public async Task UpsertBlobAsync_WithStreamInClient_UpdatesAndReturnsBlobStream()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKvStore(options => options.UseInMemoryDatabase($"kvstore-{Guid.NewGuid():N}"));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IKvStoreClientFactory>();
        var client = factory.CreateVersionedClient();
        using var createStream = new MemoryStream([10, 20]);
        var created = await client.UpsertBlobAsync("blob-versioned", null, createStream);
        using var updateStream = new MemoryStream([30, 40, 50]);

        // Act
        var updated = await client.UpsertBlobAsync("blob-versioned", created.VersionId, updateStream);
        using var readStream = await client.GetBlobStreamAsync("blob-versioned");
        using var copied = new MemoryStream();
        await readStream!.CopyToAsync(copied);

        // Assert
        await Assert.That(updated.Success).IsTrue();
        await Assert.That(Convert.ToBase64String(copied.ToArray()))
            .IsEqualTo(Convert.ToBase64String([30, 40, 50]));
    }

    private static KvStoreDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<KvStoreDbContext>()
            .UseInMemoryDatabase($"kvstore-tests-{Guid.NewGuid():N}")
            .Options;

        return new KvStoreDbContext(options);
    }
}
