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
        var stringTypeMapping = Dependencies.TypeMappingSource.FindMapping(typeof(string))
            ?? throw new InvalidOperationException("Could not resolve string type mapping.");
        var userNameLiteral = stringTypeMapping.GenerateSqlLiteral(createSqlUserOperation.UserName);
        var passwordLiteral = stringTypeMapping.GenerateSqlLiteral(password);
        var delimitedUserName = Dependencies.SqlGenerationHelper.DelimitIdentifier(createSqlUserOperation.UserName);

        if (createSqlUserOperation.IfNotExists)
        {
            builder
                .AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = {userNameLiteral})")
                .AppendLine("BEGIN");
        }

        builder
            .AppendLine($"    CREATE USER {delimitedUserName} WITH PASSWORD = {passwordLiteral};");

        if (createSqlUserOperation.IfNotExists)
        {
            builder.AppendLine("END;");
        }

        builder.EndCommand();
    }
}
