using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EfCustomMigrations;
/// <summary>
/// Extensions for wiring custom EF migration SQL generators.
/// </summary>

public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Configures custom migration operations for SQL Server.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    /// <returns>The same options builder instance.</returns>
    public static DbContextOptionsBuilder UseEfCustomMigrationOperationsForSqlServer(
        this DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.ReplaceService<IMigrationsSqlGenerator, SqlServerCustomMigrationsSqlGenerator>();
        return optionsBuilder;
    }
    /// <summary>
    /// Configures custom migration operations for SQL Server.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    /// <returns>The same options builder instance.</returns>

    public static DbContextOptionsBuilder<TContext> UseEfCustomMigrationOperationsForSqlServer<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        UseEfCustomMigrationOperationsForSqlServer((DbContextOptionsBuilder)optionsBuilder);
        return optionsBuilder;
    }
    /// <summary>
    /// Configures custom migration operations for PostgreSQL.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    /// <returns>The same options builder instance.</returns>

    public static DbContextOptionsBuilder UseEfCustomMigrationOperationsForPostgreSql(
        this DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.ReplaceService<IMigrationsSqlGenerator, PostgreSqlCustomMigrationsSqlGenerator>();
        return optionsBuilder;
    }
    /// <summary>
    /// Configures custom migration operations for PostgreSQL.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    /// <returns>The same options builder instance.</returns>

    public static DbContextOptionsBuilder<TContext> UseEfCustomMigrationOperationsForPostgreSql<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        UseEfCustomMigrationOperationsForPostgreSql((DbContextOptionsBuilder)optionsBuilder);
        return optionsBuilder;
    }
}
