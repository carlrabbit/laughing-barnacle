using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Update;

namespace EfCustomMigrations;

public sealed class SqlServerCustomMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    ICommandBatchPreparer commandBatchPreparer)
    : SqlServerMigrationsSqlGenerator(dependencies, commandBatchPreparer)
{
    protected override void Generate(MigrationOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        if (operation is not CreateSqlUserOperation createSqlUserOperation)
        {
            base.Generate(operation, model, builder);
            return;
        }

        var password = PasswordResolver.Resolve(
            createSqlUserOperation.Password,
            createSqlUserOperation.PasswordEnvironmentVariable);
        var escapedUserNameLiteral = EscapeSqlLiteral(createSqlUserOperation.UserName);
        var escapedPasswordLiteral = EscapeSqlLiteral(password);
        var delimitedUserName = Dependencies.SqlGenerationHelper.DelimitIdentifier(createSqlUserOperation.UserName);

        if (createSqlUserOperation.IfNotExists)
        {
            builder
                .AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{escapedUserNameLiteral}')")
                .AppendLine("BEGIN");
        }

        builder
            .AppendLine($"    CREATE USER {delimitedUserName} WITH PASSWORD = N'{escapedPasswordLiteral}';");

        if (createSqlUserOperation.IfNotExists)
        {
            builder.AppendLine("END;");
        }

        builder.EndCommand();
    }

    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
