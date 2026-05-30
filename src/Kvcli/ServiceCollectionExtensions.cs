using Kvstore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kvcli;
/// <summary>
/// Represents service collection extensions.
/// </summary>

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Performs the add kv store with sql server operation.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The operation result.</returns>
    public static IServiceCollection AddKvStoreWithSqlServer(
        this IServiceCollection services,
        string connectionString) =>
        services.AddKvStore(
            options => options.UseSqlServer(connectionString));
    /// <summary>
    /// Performs the add kv store with postgre sql operation.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The operation result.</returns>

    public static IServiceCollection AddKvStoreWithPostgreSql(
        this IServiceCollection services,
        string connectionString) =>
        services.AddKvStore(
            options => options.UseNpgsql(connectionString));
    /// <summary>
    /// Performs the add kv store operation.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="configureDbContext">The configure db context.</param>
    /// <returns>The operation result.</returns>

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
