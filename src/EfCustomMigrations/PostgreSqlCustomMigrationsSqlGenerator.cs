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
        var stringTypeMapping = Dependencies.TypeMappingSource.FindMapping(typeof(string))
            ?? throw new InvalidOperationException("Could not resolve string type mapping.");
        var userNameLiteral = stringTypeMapping.GenerateSqlLiteral(createSqlUserOperation.UserName);
        var passwordLiteral = stringTypeMapping.GenerateSqlLiteral(password);
        var delimitedUserName = Dependencies.SqlGenerationHelper.DelimitIdentifier(createSqlUserOperation.UserName);

        if (createSqlUserOperation.IfNotExists)
        {
            builder.AppendLine("DO $$")
                .AppendLine("BEGIN")
                .AppendLine(
                    $"    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = {userNameLiteral}) THEN");
        }

        builder.AppendLine($"        CREATE ROLE {delimitedUserName} LOGIN PASSWORD {passwordLiteral};");

        if (createSqlUserOperation.IfNotExists)
        {
            builder.AppendLine("    END IF;")
                .AppendLine("END")
                .AppendLine("$$;");
        }

        builder.EndCommand();
    }
}
