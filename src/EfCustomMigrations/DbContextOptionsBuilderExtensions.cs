using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EfCustomMigrations;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseEfCustomMigrationOperationsForSqlServer(
        this DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.ReplaceService<IMigrationsSqlGenerator, SqlServerCustomMigrationsSqlGenerator>();
        return optionsBuilder;
    }

    public static DbContextOptionsBuilder<TContext> UseEfCustomMigrationOperationsForSqlServer<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        UseEfCustomMigrationOperationsForSqlServer((DbContextOptionsBuilder)optionsBuilder);
        return optionsBuilder;
    }

    public static DbContextOptionsBuilder UseEfCustomMigrationOperationsForPostgreSql(
        this DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.ReplaceService<IMigrationsSqlGenerator, PostgreSqlCustomMigrationsSqlGenerator>();
        return optionsBuilder;
    }

    public static DbContextOptionsBuilder<TContext> UseEfCustomMigrationOperationsForPostgreSql<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        UseEfCustomMigrationOperationsForPostgreSql((DbContextOptionsBuilder)optionsBuilder);
        return optionsBuilder;
    }
}
