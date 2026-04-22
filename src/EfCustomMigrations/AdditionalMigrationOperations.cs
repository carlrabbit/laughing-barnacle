using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EfCustomMigrations;

/// <summary>
/// Documentation.
/// </summary>
public sealed class CreateSqlLoginOrRoleOperation : MigrationOperation
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public required string PrincipalName { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public string? PasswordEnvironmentVariable { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public string? DefaultSchema { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public IReadOnlyList<string> MemberOfRoles { get; init; } = [];

    /// <summary>
    /// Documentation.
    /// </summary>
    public bool IfNotExists { get; init; } = true;
}

/// <summary>
/// Documentation.
/// </summary>
public sealed class GrantSchemaOrTablePrivilegesOperation : MigrationOperation
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public required string PrincipalName { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public required string SchemaName { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public string? TableName { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public IReadOnlyList<string> Privileges { get; init; } = [];

    /// <summary>
    /// Documentation.
    /// </summary>
    public bool WithGrantOption { get; init; }
}

/// <summary>
/// Documentation.
/// </summary>
public sealed class CreateSchemaWithOwnerOperation : MigrationOperation
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public required string SchemaName { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public required string OwnerPrincipalName { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public bool IfNotExists { get; init; } = true;
}

/// <summary>
/// Documentation.
/// </summary>
public sealed class SeedIdempotentSqlOperation : MigrationOperation
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public required string SeedKey { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public required string Sql { get; init; }
}

/// <summary>
/// Documentation.
/// </summary>
public sealed class SetDatabaseOptionOperation : MigrationOperation
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public required string OptionName { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public required string OptionValue { get; init; }
}

/// <summary>
/// Documentation.
/// </summary>
public sealed class SetIndexStorageParameterOperation : MigrationOperation
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public required string SchemaName { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public required string IndexName { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public required string ParameterName { get; init; }

    /// <summary>
    /// Documentation.
    /// </summary>
    public required string ParameterValue { get; init; }
}
