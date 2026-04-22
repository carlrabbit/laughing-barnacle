using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace EfCustomMigrations;

/// <summary>
/// Documentation.
/// </summary>
public sealed class SqlServerCustomMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    ICommandBatchPreparer commandBatchPreparer)
    : SqlServerMigrationsSqlGenerator(dependencies, commandBatchPreparer)
{
    private RelationalTypeMapping? stringTypeMapping;

    /// <summary>
    /// Documentation.
    /// </summary>
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
            builder
                .AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = {userNameLiteral})")
                .AppendLine("BEGIN");
        }

        builder.AppendLine($"    CREATE USER {delimitedUserName} WITH PASSWORD = {passwordLiteral};");

        if (operation.IfNotExists)
        {
            builder.AppendLine("END;");
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
            builder.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = {principalNameLiteral})")
                .AppendLine("BEGIN");
        }

        builder.AppendLine($"    CREATE LOGIN {delimitedPrincipalName} WITH PASSWORD = {passwordLiteral};");

        if (operation.IfNotExists)
        {
            builder.AppendLine("END;");
            builder.AppendLine(
                $"IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = {principalNameLiteral})")
                .AppendLine("BEGIN");
        }

        if (string.IsNullOrWhiteSpace(operation.DefaultSchema))
        {
            builder.AppendLine($"    CREATE USER {delimitedPrincipalName} FOR LOGIN {delimitedPrincipalName};");
        }
        else
        {
            var delimitedDefaultSchema = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.DefaultSchema);
            builder.AppendLine(
                $"    CREATE USER {delimitedPrincipalName} FOR LOGIN {delimitedPrincipalName} " +
                $"WITH DEFAULT_SCHEMA = {delimitedDefaultSchema};");
        }

        if (operation.IfNotExists)
        {
            builder.AppendLine("END;");
        }

        foreach (var roleName in operation.MemberOfRoles.Where(role => !string.IsNullOrWhiteSpace(role)))
        {
            var roleNameLiteral = StringTypeMapping.GenerateSqlLiteral(roleName);
            var delimitedRoleName = Dependencies.SqlGenerationHelper.DelimitIdentifier(roleName);
            builder.AppendLine($"IF IS_ROLEMEMBER({roleNameLiteral}, {principalNameLiteral}) <> 1")
                .AppendLine("BEGIN")
                .AppendLine($"    ALTER ROLE {delimitedRoleName} ADD MEMBER {delimitedPrincipalName};")
                .AppendLine("END;");
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
            builder.AppendLine($"GRANT {privileges} ON SCHEMA::{delimitedSchemaName} TO {delimitedPrincipalName}{withGrantOption};");
        }
        else
        {
            var delimitedObject = Dependencies.SqlGenerationHelper.DelimitIdentifier(
                operation.TableName,
                operation.SchemaName);
            builder.AppendLine($"GRANT {privileges} ON OBJECT::{delimitedObject} TO {delimitedPrincipalName}{withGrantOption};");
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
            builder.AppendLine($"IF SCHEMA_ID({schemaNameLiteral}) IS NULL")
                .AppendLine("BEGIN")
                .AppendLine($"    CREATE SCHEMA {delimitedSchemaName} AUTHORIZATION {delimitedOwner};")
                .AppendLine("END;");
        }
        else
        {
            builder.AppendLine($"CREATE SCHEMA {delimitedSchemaName} AUTHORIZATION {delimitedOwner};");
        }

        builder.AppendLine($"ALTER AUTHORIZATION ON SCHEMA::{delimitedSchemaName} TO {delimitedOwner};");
        builder.EndCommand();
    }

    private void GenerateSeedIdempotentSql(SeedIdempotentSqlOperation operation, MigrationCommandListBuilder builder)
    {
        var seedKeyLiteral = StringTypeMapping.GenerateSqlLiteral(operation.SeedKey);
        builder.AppendLine("IF OBJECT_ID(N'[dbo].[__EfCustomMigrationSeedHistory]', N'U') IS NULL")
            .AppendLine("BEGIN")
            .AppendLine("    CREATE TABLE [dbo].[__EfCustomMigrationSeedHistory](")
            .AppendLine("        [SeedKey] nvarchar(450) NOT NULL PRIMARY KEY,")
            .AppendLine("        [AppliedOnUtc] datetime2 NOT NULL DEFAULT SYSUTCDATETIME()")
            .AppendLine("    );")
            .AppendLine("END;")
            .AppendLine()
            .AppendLine($"IF NOT EXISTS (SELECT 1 FROM [dbo].[__EfCustomMigrationSeedHistory] WHERE [SeedKey] = {seedKeyLiteral})")
            .AppendLine("BEGIN")
            .AppendLine(operation.Sql.Trim())
            .AppendLine($"    INSERT INTO [dbo].[__EfCustomMigrationSeedHistory]([SeedKey]) VALUES ({seedKeyLiteral});")
            .AppendLine("END;");

        builder.EndCommand();
    }

    private void GenerateSetDatabaseOption(SetDatabaseOptionOperation operation, MigrationCommandListBuilder builder)
    {
        if (operation.OptionName.Equals("Collation", StringComparison.OrdinalIgnoreCase))
        {
            builder.AppendLine($"ALTER DATABASE CURRENT COLLATE {operation.OptionValue};");
        }
        else if (operation.OptionName.Equals("CompatibilityLevel", StringComparison.OrdinalIgnoreCase))
        {
            builder.AppendLine($"ALTER DATABASE CURRENT SET COMPATIBILITY_LEVEL = {operation.OptionValue};");
        }
        else
        {
            builder.AppendLine($"ALTER DATABASE CURRENT SET {operation.OptionName} {operation.OptionValue};");
        }

        builder.EndCommand();
    }

    private void GenerateSetIndexStorageParameter(
        SetIndexStorageParameterOperation operation,
        MigrationCommandListBuilder builder)
    {
        var delimitedIndexName = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.IndexName);
        var delimitedTableName = Dependencies.SqlGenerationHelper.DelimitIdentifier(
            operation.TableName,
            operation.SchemaName);
        builder.AppendLine(
            $"ALTER INDEX {delimitedIndexName} ON {delimitedTableName} " +
            $"REBUILD WITH ({operation.ParameterName} = {operation.ParameterValue});");

        builder.EndCommand();
    }

    private RelationalTypeMapping StringTypeMapping =>
        stringTypeMapping ??= Dependencies.TypeMappingSource.FindMapping(typeof(string))
        ?? throw new InvalidOperationException("Could not resolve string type mapping.");
}
