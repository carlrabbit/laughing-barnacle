using Kvstore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kvcli;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKvStoreWithSqlServer(
        this IServiceCollection services,
        string connectionString) =>
        services.AddKvStore(
            options => options.UseSqlServer(connectionString));

    public static IServiceCollection AddKvStoreWithPostgreSql(
        this IServiceCollection services,
        string connectionString) =>
        services.AddKvStore(
            options => options.UseNpgsql(connectionString));

    public static IServiceCollection AddKvStore(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        services.AddDbContext<KvStoreDbContext>(configureDbContext);
        services.AddScoped<IWriteOnceKeyValueStore, WriteOnceKeyValueStore>();
        services.AddScoped<IVersionedKeyValueStore, VersionedKeyValueStore>();
        services.AddScoped<IWriteOnceKvClient, WriteOnceKvClient>();
        services.AddScoped<IVersionedKvClient, VersionedKvClient>();
        services.AddScoped<IKvStoreClientFactory, KvStoreClientFactory>();
        return services;
    }
}
