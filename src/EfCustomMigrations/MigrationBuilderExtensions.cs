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
                nameof(password));
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
}
