using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EfCustomMigrations;
/// <summary>
/// Migration operation that creates a SQL login or role.
/// </summary>

public sealed class CreateSqlLoginOrRoleOperation : MigrationOperation
{
    /// <summary>
    /// Gets the login or role name.
    /// </summary>
    public required string PrincipalName { get; init; }
    /// <summary>
    /// Gets the explicit password, if provided.
    /// </summary>

    public string? Password { get; init; }
    /// <summary>
    /// Gets the environment variable name containing the password.
    /// </summary>

    public string? PasswordEnvironmentVariable { get; init; }
    /// <summary>
    /// Gets the default schema assigned to the principal.
    /// </summary>

    public string? DefaultSchema { get; init; }
    /// <summary>
    /// Gets the roles the principal should be added to.
    /// </summary>

    public IReadOnlyList<string> MemberOfRoles { get; init; } = [];
    /// <summary>
    /// Gets a value indicating whether creation should be conditional when supported.
    /// </summary>

    public bool IfNotExists { get; init; } = true;
}
/// <summary>
/// Migration operation that grants privileges on a schema or table.
/// </summary>

public sealed class GrantSchemaOrTablePrivilegesOperation : MigrationOperation
{
    /// <summary>
    /// Gets the principal receiving the privileges.
    /// </summary>
    public required string PrincipalName { get; init; }
    /// <summary>
    /// Gets the schema name.
    /// </summary>

    public required string SchemaName { get; init; }
    /// <summary>
    /// Gets the optional table name.
    /// </summary>

    public string? TableName { get; init; }
    /// <summary>
    /// Gets the privileges to grant.
    /// </summary>

    public IReadOnlyList<string> Privileges { get; init; } = [];
    /// <summary>
    /// Gets a value indicating whether <c>WITH GRANT OPTION</c> should be applied.
    /// </summary>

    public bool WithGrantOption { get; init; }
}
/// <summary>
/// Migration operation that creates a schema and assigns an owner.
/// </summary>

public sealed class CreateSchemaWithOwnerOperation : MigrationOperation
{
    /// <summary>
    /// Gets the schema name to create.
    /// </summary>
    public required string SchemaName { get; init; }
    /// <summary>
    /// Gets the owner principal name.
    /// </summary>

    public required string OwnerPrincipalName { get; init; }
    /// <summary>
    /// Gets a value indicating whether creation should be conditional when supported.
    /// </summary>

    public bool IfNotExists { get; init; } = true;
}
/// <summary>
/// Migration operation that executes idempotent seed SQL tracked by a seed key.
/// </summary>

public sealed class SeedIdempotentSqlOperation : MigrationOperation
{
    /// <summary>
    /// Gets the unique key used to track whether the seed has been applied.
    /// </summary>
    public required string SeedKey { get; init; }
    /// <summary>
    /// Gets the SQL to execute for seeding.
    /// </summary>

    public required string Sql { get; init; }
}
/// <summary>
/// Migration operation that sets a database-level option.
/// </summary>

public sealed class SetDatabaseOptionOperation : MigrationOperation
{
    /// <summary>
    /// Gets the database option name.
    /// </summary>
    public required string OptionName { get; init; }
    /// <summary>
    /// Gets the database option value.
    /// </summary>

    public required string OptionValue { get; init; }
}
/// <summary>
/// Migration operation that sets an index storage parameter.
/// </summary>

public sealed class SetIndexStorageParameterOperation : MigrationOperation
{
    /// <summary>
    /// Gets or sets the schema name.
    /// </summary>
    public required string SchemaName { get; init; }
    /// <summary>
    /// Gets or sets the table name.
    /// </summary>

    public required string TableName { get; init; }
    /// <summary>
    /// Gets or sets the index name.
    /// </summary>

    public required string IndexName { get; init; }
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>

    public required string ParameterName { get; init; }
    /// <summary>
    /// Gets or sets the parameter value.
    /// </summary>

    public required string ParameterValue { get; init; }
}
