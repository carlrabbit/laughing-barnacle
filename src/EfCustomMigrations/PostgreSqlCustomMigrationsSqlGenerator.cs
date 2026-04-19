using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace EfCustomMigrations;

public sealed class PostgreSqlCustomMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    INpgsqlSingletonOptions npgsqlSingletonOptions)
    : NpgsqlMigrationsSqlGenerator(dependencies, npgsqlSingletonOptions)
{
    private RelationalTypeMapping? stringTypeMapping;

    protected override void Generate(MigrationOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        switch (operation)
        {
            case CreateSqlUserOperation createSqlUserOperation:
                GenerateCreateSqlUser(createSqlUserOperation, builder);
                break;
            case CreateSqlLoginOrRoleOperation createSqlLoginOrRoleOperation:
                GenerateCreateSqlLoginOrRole(createSqlLoginOrRoleOperation, builder);
                break;
            case GrantSchemaOrTablePrivilegesOperation grantOperation:
                GenerateGrantSchemaOrTablePrivileges(grantOperation, builder);
                break;
            case CreateSchemaWithOwnerOperation createSchemaWithOwnerOperation:
                GenerateCreateSchemaWithOwner(createSchemaWithOwnerOperation, builder);
                break;
            case SeedIdempotentSqlOperation seedIdempotentSqlOperation:
                GenerateSeedIdempotentSql(seedIdempotentSqlOperation, builder);
                break;
            case SetDatabaseOptionOperation setDatabaseOptionOperation:
                GenerateSetDatabaseOption(setDatabaseOptionOperation, builder);
                break;
            case SetIndexStorageParameterOperation setIndexStorageParameterOperation:
                GenerateSetIndexStorageParameter(setIndexStorageParameterOperation, builder);
                break;
            default:
                base.Generate(operation, model, builder);
                break;
        }
    }

    private void GenerateCreateSqlUser(CreateSqlUserOperation operation, MigrationCommandListBuilder builder)
    {
        var password = PasswordResolver.Resolve(
            operation.Password,
            operation.PasswordEnvironmentVariable);
        var userNameLiteral = StringTypeMapping.GenerateSqlLiteral(operation.UserName);
        var passwordLiteral = StringTypeMapping.GenerateSqlLiteral(password);
        var delimitedUserName = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.UserName);

        if (operation.IfNotExists)
        {
            builder.AppendLine("DO $$")
                .AppendLine("BEGIN")
                .AppendLine($"    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = {userNameLiteral}) THEN");
        }

        builder.AppendLine($"        CREATE ROLE {delimitedUserName} LOGIN PASSWORD {passwordLiteral};");

        if (operation.IfNotExists)
        {
            builder.AppendLine("    END IF;")
                .AppendLine("END")
                .AppendLine("$$;");
        }

        builder.EndCommand();
    }

    private void GenerateCreateSqlLoginOrRole(CreateSqlLoginOrRoleOperation operation, MigrationCommandListBuilder builder)
    {
        var password = PasswordResolver.Resolve(operation.Password, operation.PasswordEnvironmentVariable);
        var principalNameLiteral = StringTypeMapping.GenerateSqlLiteral(operation.PrincipalName);
        var passwordLiteral = StringTypeMapping.GenerateSqlLiteral(password);
        var delimitedPrincipalName = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.PrincipalName);

        if (operation.IfNotExists)
        {
            builder.AppendLine("DO $$")
                .AppendLine("BEGIN")
                .AppendLine(
                    $"    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = {principalNameLiteral}) THEN");
        }

        builder.AppendLine($"        CREATE ROLE {delimitedPrincipalName} LOGIN PASSWORD {passwordLiteral};");

        if (operation.IfNotExists)
        {
            builder.AppendLine("    END IF;")
                .AppendLine("END")
                .AppendLine("$$;");
        }

        foreach (var roleName in operation.MemberOfRoles.Where(role => !string.IsNullOrWhiteSpace(role)))
        {
            var delimitedRoleName = Dependencies.SqlGenerationHelper.DelimitIdentifier(roleName);
            builder.AppendLine($"GRANT {delimitedRoleName} TO {delimitedPrincipalName};");
        }

        builder.EndCommand();
    }

    private void GenerateGrantSchemaOrTablePrivileges(
        GrantSchemaOrTablePrivilegesOperation operation,
        MigrationCommandListBuilder builder)
    {
        var delimitedPrincipalName = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.PrincipalName);
        var privileges = string.Join(", ", operation.Privileges);
        var withGrantOption = operation.WithGrantOption ? " WITH GRANT OPTION" : string.Empty;

        if (string.IsNullOrWhiteSpace(operation.TableName))
        {
            var delimitedSchemaName = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.SchemaName);
            builder.AppendLine($"GRANT {privileges} ON SCHEMA {delimitedSchemaName} TO {delimitedPrincipalName}{withGrantOption};");
        }
        else
        {
            var delimitedTableName = Dependencies.SqlGenerationHelper.DelimitIdentifier(
                operation.TableName,
                operation.SchemaName);
            builder.AppendLine($"GRANT {privileges} ON TABLE {delimitedTableName} TO {delimitedPrincipalName}{withGrantOption};");
        }

        builder.EndCommand();
    }

    private void GenerateCreateSchemaWithOwner(CreateSchemaWithOwnerOperation operation, MigrationCommandListBuilder builder)
    {
        var schemaNameLiteral = StringTypeMapping.GenerateSqlLiteral(operation.SchemaName);
        var delimitedSchemaName = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.SchemaName);
        var delimitedOwner = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.OwnerPrincipalName);

        if (operation.IfNotExists)
        {
            builder.AppendLine("DO $$")
                .AppendLine("BEGIN")
                .AppendLine($"    IF NOT EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = {schemaNameLiteral}) THEN")
                .AppendLine($"        CREATE SCHEMA {delimitedSchemaName} AUTHORIZATION {delimitedOwner};")
                .AppendLine("    END IF;")
                .AppendLine("END")
                .AppendLine("$$;");
        }
        else
        {
            builder.AppendLine($"CREATE SCHEMA {delimitedSchemaName} AUTHORIZATION {delimitedOwner};");
        }

        builder.AppendLine($"ALTER SCHEMA {delimitedSchemaName} OWNER TO {delimitedOwner};");
        builder.EndCommand();
    }

    private void GenerateSeedIdempotentSql(SeedIdempotentSqlOperation operation, MigrationCommandListBuilder builder)
    {
        var seedKeyLiteral = StringTypeMapping.GenerateSqlLiteral(operation.SeedKey);
        builder.AppendLine(
                "CREATE TABLE IF NOT EXISTS public.\"__EfCustomMigrationSeedHistory\"(")
            .AppendLine("    \"SeedKey\" text PRIMARY KEY,")
            .AppendLine("    \"AppliedOnUtc\" timestamptz NOT NULL DEFAULT timezone('utc', now())")
            .AppendLine(");")
            .AppendLine()
            .AppendLine("DO $$")
            .AppendLine("BEGIN")
            .AppendLine(
                $"    IF NOT EXISTS (SELECT 1 FROM public.\"__EfCustomMigrationSeedHistory\" WHERE \"SeedKey\" = {seedKeyLiteral}) THEN")
            .AppendLine(operation.Sql.Trim())
            .AppendLine(
                $"        INSERT INTO public.\"__EfCustomMigrationSeedHistory\"(\"SeedKey\") VALUES ({seedKeyLiteral});")
            .AppendLine("    END IF;")
            .AppendLine("END")
            .AppendLine("$$;");

        builder.EndCommand();
    }

    private void GenerateSetDatabaseOption(SetDatabaseOptionOperation operation, MigrationCommandListBuilder builder)
    {
        if (operation.OptionName.Equals("Collation", StringComparison.OrdinalIgnoreCase) ||
            operation.OptionName.Equals("CompatibilityLevel", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The '{operation.OptionName}' option is not supported through ALTER DATABASE in PostgreSQL.");
        }

        var optionValueLiteral = StringTypeMapping.GenerateSqlLiteral(operation.OptionValue);
        builder.AppendLine($"ALTER DATABASE CURRENT SET {operation.OptionName} = {optionValueLiteral};");
        builder.EndCommand();
    }

    private void GenerateSetIndexStorageParameter(
        SetIndexStorageParameterOperation operation,
        MigrationCommandListBuilder builder)
    {
        var delimitedIndexName = Dependencies.SqlGenerationHelper.DelimitIdentifier(
            operation.IndexName,
            operation.SchemaName);
        builder.AppendLine(
            $"ALTER INDEX {delimitedIndexName} SET ({operation.ParameterName} = {operation.ParameterValue});");
        builder.EndCommand();
    }

    private RelationalTypeMapping StringTypeMapping =>
        stringTypeMapping ??= Dependencies.TypeMappingSource.FindMapping(typeof(string))
        ?? throw new InvalidOperationException("Could not resolve string type mapping.");
}
