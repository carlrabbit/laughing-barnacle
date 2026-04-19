using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace EfCustomMigrations;

public static class MigrationBuilderExtensions
{
    public static OperationBuilder<CreateSqlUserOperation> CreateSqlUser(
        this MigrationBuilder migrationBuilder,
        string userName,
        string? password = null,
        string? passwordEnvironmentVariable = null,
        bool ifNotExists = true)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        if (string.IsNullOrWhiteSpace(password) && string.IsNullOrWhiteSpace(passwordEnvironmentVariable))
        {
            throw new ArgumentException(
                "Either a password or a password environment variable must be provided.",
                $"{nameof(password)}|{nameof(passwordEnvironmentVariable)}");
        }

        var operation = new CreateSqlUserOperation
        {
            UserName = userName,
            Password = password,
            PasswordEnvironmentVariable = passwordEnvironmentVariable,
            IfNotExists = ifNotExists
        };

        migrationBuilder.Operations.Add(operation);
        return new OperationBuilder<CreateSqlUserOperation>(operation);
    }

    public static OperationBuilder<CreateSqlLoginOrRoleOperation> CreateSqlLoginOrRole(
        this MigrationBuilder migrationBuilder,
        string principalName,
        string? password = null,
        string? passwordEnvironmentVariable = null,
        string? defaultSchema = null,
        IReadOnlyList<string>? memberOfRoles = null,
        bool ifNotExists = true)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(principalName);

        if (string.IsNullOrWhiteSpace(password) && string.IsNullOrWhiteSpace(passwordEnvironmentVariable))
        {
            throw new ArgumentException(
                "Either a password or a password environment variable must be provided.",
                $"{nameof(password)}|{nameof(passwordEnvironmentVariable)}");
        }

        var operation = new CreateSqlLoginOrRoleOperation
        {
            PrincipalName = principalName,
            Password = password,
            PasswordEnvironmentVariable = passwordEnvironmentVariable,
            DefaultSchema = defaultSchema,
            MemberOfRoles = memberOfRoles ?? [],
            IfNotExists = ifNotExists
        };

        migrationBuilder.Operations.Add(operation);
        return new OperationBuilder<CreateSqlLoginOrRoleOperation>(operation);
    }

    public static OperationBuilder<GrantSchemaOrTablePrivilegesOperation> GrantSchemaOrTablePrivileges(
        this MigrationBuilder migrationBuilder,
        string principalName,
        string schemaName,
        IReadOnlyList<string> privileges,
        string? tableName = null,
        bool withGrantOption = false)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(principalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentNullException.ThrowIfNull(privileges);

        if (privileges.Count == 0)
        {
            throw new ArgumentException("At least one privilege must be provided.", nameof(privileges));
        }

        var operation = new GrantSchemaOrTablePrivilegesOperation
        {
            PrincipalName = principalName,
            SchemaName = schemaName,
            TableName = tableName,
            Privileges = privileges,
            WithGrantOption = withGrantOption
        };

        migrationBuilder.Operations.Add(operation);
        return new OperationBuilder<GrantSchemaOrTablePrivilegesOperation>(operation);
    }

    public static OperationBuilder<CreateSchemaWithOwnerOperation> CreateSchemaWithOwner(
        this MigrationBuilder migrationBuilder,
        string schemaName,
        string ownerPrincipalName,
        bool ifNotExists = true)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerPrincipalName);

        var operation = new CreateSchemaWithOwnerOperation
        {
            SchemaName = schemaName,
            OwnerPrincipalName = ownerPrincipalName,
            IfNotExists = ifNotExists
        };

        migrationBuilder.Operations.Add(operation);
        return new OperationBuilder<CreateSchemaWithOwnerOperation>(operation);
    }

    public static OperationBuilder<SeedIdempotentSqlOperation> SeedIdempotentSql(
        this MigrationBuilder migrationBuilder,
        string seedKey,
        string sql)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(seedKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        var operation = new SeedIdempotentSqlOperation
        {
            SeedKey = seedKey,
            Sql = sql
        };

        migrationBuilder.Operations.Add(operation);
        return new OperationBuilder<SeedIdempotentSqlOperation>(operation);
    }

    public static OperationBuilder<SetDatabaseOptionOperation> SetDatabaseOption(
        this MigrationBuilder migrationBuilder,
        string optionName,
        string optionValue)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(optionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(optionValue);

        var operation = new SetDatabaseOptionOperation
        {
            OptionName = optionName,
            OptionValue = optionValue
        };

        migrationBuilder.Operations.Add(operation);
        return new OperationBuilder<SetDatabaseOptionOperation>(operation);
    }

    public static OperationBuilder<SetIndexStorageParameterOperation> SetIndexStorageParameter(
        this MigrationBuilder migrationBuilder,
        string schemaName,
        string tableName,
        string indexName,
        string parameterName,
        string parameterValue)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(indexName);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterValue);

        var operation = new SetIndexStorageParameterOperation
        {
            SchemaName = schemaName,
            TableName = tableName,
            IndexName = indexName,
            ParameterName = parameterName,
            ParameterValue = parameterValue
        };

        migrationBuilder.Operations.Add(operation);
        return new OperationBuilder<SetIndexStorageParameterOperation>(operation);
    }
}
