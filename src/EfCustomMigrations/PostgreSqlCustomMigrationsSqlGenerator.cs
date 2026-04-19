using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace EfCustomMigrations;

public sealed class PostgreSqlCustomMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    INpgsqlSingletonOptions npgsqlSingletonOptions)
    : NpgsqlMigrationsSqlGenerator(dependencies, npgsqlSingletonOptions)
{
    protected override void Generate(MigrationOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        if (operation is not CreateSqlUserOperation createSqlUserOperation)
        {
            base.Generate(operation, model, builder);
            return;
        }

        var password = PasswordResolver.Resolve(
            createSqlUserOperation.Password,
            createSqlUserOperation.PasswordEnvironmentVariable);
        var escapedUserNameLiteral = EscapeSqlLiteral(createSqlUserOperation.UserName);
        var escapedPasswordLiteral = EscapeSqlLiteral(password);
        var delimitedUserName = Dependencies.SqlGenerationHelper.DelimitIdentifier(createSqlUserOperation.UserName);

        if (createSqlUserOperation.IfNotExists)
        {
            builder.AppendLine("DO $$")
                .AppendLine("BEGIN")
                .AppendLine(
                    $"    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = '{escapedUserNameLiteral}') THEN");
        }

        builder.AppendLine($"        CREATE ROLE {delimitedUserName} LOGIN PASSWORD '{escapedPasswordLiteral}';");

        if (createSqlUserOperation.IfNotExists)
        {
            builder.AppendLine("    END IF;")
                .AppendLine("END")
                .AppendLine("$$;");
        }

        builder.EndCommand();
    }

    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
