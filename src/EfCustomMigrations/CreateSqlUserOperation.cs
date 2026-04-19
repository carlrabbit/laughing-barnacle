using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EfCustomMigrations;

public sealed class CreateSqlUserOperation : MigrationOperation
{
    public required string UserName { get; init; }

    public string? Password { get; init; }

    public string? PasswordEnvironmentVariable { get; init; }

    public bool IfNotExists { get; init; } = true;
}
