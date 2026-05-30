using Binstore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bincli;
/// <summary>
/// Represents service collection extensions.
/// </summary>

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Performs the add bin store with sql server operation.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The operation result.</returns>
    public static IServiceCollection AddBinStoreWithSqlServer(
        this IServiceCollection services,
        string connectionString) =>
        services.AddBinStore(
            options => options.UseSqlServer(connectionString));
    /// <summary>
    /// Performs the add bin store with postgre sql operation.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The operation result.</returns>

    public static IServiceCollection AddBinStoreWithPostgreSql(
        this IServiceCollection services,
        string connectionString) =>
        services.AddBinStore(
            options => options.UseNpgsql(connectionString));
    /// <summary>
    /// Performs the add bin store operation.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="configureDbContext">The configure db context.</param>
    /// <returns>The operation result.</returns>

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
