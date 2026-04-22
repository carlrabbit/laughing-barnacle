using Jsonstore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jsoncli;

/// <summary>
/// Documentation.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public static IServiceCollection AddJsonStoreWithSqlServer(
        this IServiceCollection services,
        string connectionString) =>
        services.AddJsonStore(
            options => options.UseSqlServer(connectionString));

    /// <summary>
    /// Documentation.
    /// </summary>
    public static IServiceCollection AddJsonStoreWithPostgreSql(
        this IServiceCollection services,
        string connectionString) =>
        services.AddJsonStore(
            options => options.UseNpgsql(connectionString));

    /// <summary>
    /// Documentation.
    /// </summary>
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
