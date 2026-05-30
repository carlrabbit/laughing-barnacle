using Jsonstore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jsoncli;
/// <summary>
/// Represents service collection extensions.
/// </summary>

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Performs the add json store with sql server operation.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The operation result.</returns>
    public static IServiceCollection AddJsonStoreWithSqlServer(
        this IServiceCollection services,
        string connectionString) =>
        services.AddJsonStore(
            options => options.UseSqlServer(connectionString));
    /// <summary>
    /// Performs the add json store with postgre sql operation.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The operation result.</returns>

    public static IServiceCollection AddJsonStoreWithPostgreSql(
        this IServiceCollection services,
        string connectionString) =>
        services.AddJsonStore(
            options => options.UseNpgsql(connectionString));
    /// <summary>
    /// Performs the add json store operation.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="configureDbContext">The configure db context.</param>
    /// <returns>The operation result.</returns>

    public static IServiceCollection AddJsonStore(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        services.AddDbContext<JsonStoreDbContext>(configureDbContext);
        services.AddScoped<IWriteOnceJsonStore, WriteOnceJsonStore>();
        services.AddScoped<IJsonClient, JsonClient>();
        services.AddScoped<IJsonClientFactory, JsonClientFactory>();
        return services;
    }
}
