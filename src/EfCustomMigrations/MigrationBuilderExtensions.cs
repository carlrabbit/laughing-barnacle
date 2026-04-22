using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace EfCustomMigrations;
/// <summary>
/// Represents migration builder extensions.
/// </summary>

public static class MigrationBuilderExtensions
{
    /// <summary>
    /// Performs the create sql user operation.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <param name="userName">The user name.</param>
    /// <param name="password">The password.</param>
    /// <param name="passwordEnvironmentVariable">The password environment variable.</param>
    /// <param name="ifNotExists">The if not exists.</param>
    /// <returns>The operation result.</returns>
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
    /// <summary>
    /// Performs the create sql login or role operation.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <param name="principalName">The principal name.</param>
    /// <param name="password">The password.</param>
    /// <param name="passwordEnvironmentVariable">The password environment variable.</param>
    /// <param name="defaultSchema">The default schema.</param>
    /// <param name="memberOfRoles">The member of roles.</param>
    /// <param name="ifNotExists">The if not exists.</param>
    /// <returns>The operation result.</returns>

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
    /// <summary>
    /// Performs the grant schema or table privileges operation.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <param name="principalName">The principal name.</param>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="privileges">The privileges.</param>
    /// <param name="tableName">The table name.</param>
    /// <param name="withGrantOption">The with grant option.</param>
    /// <returns>The operation result.</returns>

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
    /// <summary>
    /// Performs the create schema with owner operation.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="ownerPrincipalName">The owner principal name.</param>
    /// <param name="ifNotExists">The if not exists.</param>
    /// <returns>The operation result.</returns>

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
    /// <summary>
    /// Performs the seed idempotent sql operation.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <param name="seedKey">The seed key.</param>
    /// <param name="sql">The sql.</param>
    /// <returns>The operation result.</returns>

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
    /// <summary>
    /// Performs the set database option operation.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <param name="optionName">The option name.</param>
    /// <param name="optionValue">The option value.</param>
    /// <returns>The operation result.</returns>

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
    /// <summary>
    /// Performs the set index storage parameter operation.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="tableName">The table name.</param>
    /// <param name="indexName">The index name.</param>
    /// <param name="parameterName">The parameter name.</param>
    /// <param name="parameterValue">The parameter value.</param>
    /// <returns>The operation result.</returns>

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
