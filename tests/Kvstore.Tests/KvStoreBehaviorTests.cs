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

    private static KvStoreDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<KvStoreDbContext>()
            .UseInMemoryDatabase($"kvstore-tests-{Guid.NewGuid():N}")
            .Options;

        return new KvStoreDbContext(options);
    }
}
