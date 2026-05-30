using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EfCustomMigrations;
/// <summary>
/// Represents create sql user operation.
/// </summary>

public sealed class CreateSqlUserOperation : MigrationOperation
{
    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public required string UserName { get; init; }
    /// <summary>
    /// Gets or sets the password.
    /// </summary>

    public string? Password { get; init; }
    /// <summary>
    /// Gets or sets the password environment variable.
    /// </summary>

    public string? PasswordEnvironmentVariable { get; init; }
    /// <summary>
    /// Gets or sets the if not exists.
    /// </summary>

    public bool IfNotExists { get; init; } = true;
}
