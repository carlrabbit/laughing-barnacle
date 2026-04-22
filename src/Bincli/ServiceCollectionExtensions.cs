using Binstore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bincli;

/// <summary>
/// Documentation.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public static IServiceCollection AddBinStoreWithSqlServer(
        this IServiceCollection services,
        string connectionString) =>
        services.AddBinStore(
            options => options.UseSqlServer(connectionString));

    /// <summary>
    /// Documentation.
    /// </summary>
    public static IServiceCollection AddBinStoreWithPostgreSql(
        this IServiceCollection services,
        string connectionString) =>
        services.AddBinStore(
            options => options.UseNpgsql(connectionString));

    /// <summary>
    /// Documentation.
    /// </summary>
    public static IServiceCollection AddBinStore(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        services.AddDbContext<BinStoreDbContext>(configureDbContext);
        services.AddScoped<IWriteOnceBinaryStore, WriteOnceBinaryStore>();
        services.AddScoped<IBinClient, BinClient>();
        services.AddScoped<IBinClientFactory, BinClientFactory>();
        return services;
    }
}
