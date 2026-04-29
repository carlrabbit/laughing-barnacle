using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EfCustomMigrations;

public sealed class CreateSqlLoginOrRoleOperation : MigrationOperation
{
    public required string PrincipalName { get; init; }

    public string? Password { get; init; }

    public string? PasswordEnvironmentVariable { get; init; }

    public string? DefaultSchema { get; init; }

    public IReadOnlyList<string> MemberOfRoles { get; init; } = [];

    public bool IfNotExists { get; init; } = true;
}

public sealed class GrantSchemaOrTablePrivilegesOperation : MigrationOperation
{
    public required string PrincipalName { get; init; }

    public required string SchemaName { get; init; }

    public string? TableName { get; init; }

    public IReadOnlyList<string> Privileges { get; init; } = [];

    public bool WithGrantOption { get; init; }
}

public sealed class CreateSchemaWithOwnerOperation : MigrationOperation
{
    public required string SchemaName { get; init; }

    public required string OwnerPrincipalName { get; init; }

    public bool IfNotExists { get; init; } = true;
}

public sealed class SeedIdempotentSqlOperation : MigrationOperation
{
    public required string SeedKey { get; init; }

    public required string Sql { get; init; }
}

public sealed class SetDatabaseOptionOperation : MigrationOperation
{
    public required string OptionName { get; init; }

    public required string OptionValue { get; init; }
}

public sealed class SetIndexStorageParameterOperation : MigrationOperation
{
    public required string SchemaName { get; init; }

    public required string TableName { get; init; }

    public required string IndexName { get; init; }

    public required string ParameterName { get; init; }

    public required string ParameterValue { get; init; }
}

public sealed class EnableSnapshotIsolationOperation : MigrationOperation
{
    public bool AllowSnapshotIsolation { get; init; } = true;

    public bool ReadCommittedSnapshot { get; init; } = true;
}
