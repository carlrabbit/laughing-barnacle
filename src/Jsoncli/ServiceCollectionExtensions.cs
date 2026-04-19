using Jsonstore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jsoncli;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJsonStoreWithSqlServer(
        this IServiceCollection services,
        string connectionString) =>
        services.AddJsonStore(
            options => options.UseSqlServer(connectionString));

    public static IServiceCollection AddJsonStoreWithPostgreSql(
        this IServiceCollection services,
        string connectionString) =>
        services.AddJsonStore(
            options => options.UseNpgsql(connectionString));

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
