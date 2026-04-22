using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EfCustomMigrations;

/// <summary>
/// Represents a custom migration operation that creates a SQL database user.
/// </summary>
public sealed class CreateSqlUserOperation : MigrationOperation
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public required string UserName { get; init; }

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
    public bool IfNotExists { get; init; } = true;
}
