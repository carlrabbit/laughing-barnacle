using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EfCustomMigrations.Tests;

public class CreateSqlUserOperationTests
{
    [Test]
    public async Task CreateSqlUser_WhenNoPasswordSourceProvided_ThrowsArgumentException()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        // Act / Assert
        await Assert.That(() => migrationBuilder.CreateSqlUser("app_user"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task GenerateSqlServer_WithExplicitPassword_UsesCustomSqlGenerator()
    {
        // Arrange
        using var dbContext = CreateSqlServerDbContext();
        var generator = dbContext.GetService<IMigrationsSqlGenerator>();
        var operation = new CreateSqlUserOperation
        {
            UserName = "app_user",
            Password = "P@ssw0rd!"
        };

        // Act
        var sql = GenerateSql(generator, [operation]);

        // Assert
        await Assert.That(generator.GetType()).IsEqualTo(typeof(SqlServerCustomMigrationsSqlGenerator));
        await Assert.That(sql).Contains("CREATE USER [app_user] WITH PASSWORD = N'P@ssw0rd!';");
        await Assert.That(sql).Contains("IF NOT EXISTS");
    }

    [Test]
    public async Task GeneratePostgreSql_WithEnvironmentVariablePassword_UsesResolvedPassword()
    {
        // Arrange
        const string passwordVariable = "EF_CUSTOM_MIGRATION_TEST_PASSWORD";
        var originalValue = Environment.GetEnvironmentVariable(passwordVariable);
        Environment.SetEnvironmentVariable(passwordVariable, "pg-secret");

        try
        {
            using var dbContext = CreatePostgreSqlDbContext();
            var generator = dbContext.GetService<IMigrationsSqlGenerator>();
            var operation = new CreateSqlUserOperation
            {
                UserName = "app_user",
                PasswordEnvironmentVariable = passwordVariable
            };

            // Act
            var sql = GenerateSql(generator, [operation]);

            // Assert
            await Assert.That(generator.GetType()).IsEqualTo(typeof(PostgreSqlCustomMigrationsSqlGenerator));
            await Assert.That(sql).Contains("CREATE ROLE app_user LOGIN PASSWORD 'pg-secret';");
            await Assert.That(sql).Contains("DO $$");
        }
        finally
        {
            Environment.SetEnvironmentVariable(passwordVariable, originalValue);
        }
    }

    [Test]
    public async Task GeneratePostgreSql_WhenEnvironmentVariableMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        const string passwordVariable = "EF_CUSTOM_MIGRATION_TEST_PASSWORD_MISSING";
        var originalValue = Environment.GetEnvironmentVariable(passwordVariable);
        Environment.SetEnvironmentVariable(passwordVariable, null);
        using var dbContext = CreatePostgreSqlDbContext();
        var generator = dbContext.GetService<IMigrationsSqlGenerator>();
        var operation = new CreateSqlUserOperation
        {
            UserName = "app_user",
            PasswordEnvironmentVariable = passwordVariable
        };

        try
        {
            // Act / Assert
            await Assert.That(() => GenerateSql(generator, [operation])).Throws<InvalidOperationException>();
        }
        finally
        {
            Environment.SetEnvironmentVariable(passwordVariable, originalValue);
        }
    }

    [Test]
    public async Task GenerateSqlServer_ForAdditionalOperations_GeneratesExpectedStatements()
    {
        // Arrange
        using var dbContext = CreateSqlServerDbContext();
        var generator = dbContext.GetService<IMigrationsSqlGenerator>();
        var operations = new MigrationOperation[]
        {
            new CreateSqlLoginOrRoleOperation
            {
                PrincipalName = "app_role_user",
                Password = "sql-secret",
                DefaultSchema = "app",
                MemberOfRoles = ["db_datareader", "db_datawriter"]
            },
            new GrantSchemaOrTablePrivilegesOperation
            {
                PrincipalName = "app_role_user",
                SchemaName = "app",
                TableName = "orders",
                Privileges = ["SELECT", "UPDATE"]
            },
            new CreateSchemaWithOwnerOperation
            {
                SchemaName = "app",
                OwnerPrincipalName = "app_role_user"
            },
            new SeedIdempotentSqlOperation
            {
                SeedKey = "seed-security",
                Sql = "INSERT INTO [app].[SecurityClaims]([Name]) VALUES (N'admin');"
            },
            new SetDatabaseOptionOperation
            {
                OptionName = "CompatibilityLevel",
                OptionValue = "160"
            },
            new SetIndexStorageParameterOperation
            {
                SchemaName = "app",
                TableName = "orders",
                IndexName = "IX_orders_created_on",
                ParameterName = "FILLFACTOR",
                ParameterValue = "80"
            }
        };

        // Act
        var sql = GenerateSql(generator, operations);

        // Assert
        await Assert.That(sql).Contains("CREATE LOGIN [app_role_user]");
        await Assert.That(sql).Contains("ALTER ROLE [db_datareader] ADD MEMBER [app_role_user]");
        await Assert.That(sql).Contains("GRANT SELECT, UPDATE ON OBJECT::[app].[orders] TO [app_role_user]");
        await Assert.That(sql).Contains("CREATE SCHEMA [app] AUTHORIZATION [app_role_user]");
        await Assert.That(sql).Contains("__EfCustomMigrationSeedHistory");
        await Assert.That(sql).Contains("SET COMPATIBILITY_LEVEL = 160");
        await Assert.That(sql).Contains("REBUILD WITH (FILLFACTOR = 80)");
    }

    [Test]
    public async Task GeneratePostgreSql_ForAdditionalOperations_GeneratesExpectedStatements()
    {
        // Arrange
        using var dbContext = CreatePostgreSqlDbContext();
        var generator = dbContext.GetService<IMigrationsSqlGenerator>();
        var operations = new MigrationOperation[]
        {
            new CreateSqlLoginOrRoleOperation
            {
                PrincipalName = "app_role_user",
                Password = "pg-secret",
                MemberOfRoles = ["pg_read_all_data"]
            },
            new GrantSchemaOrTablePrivilegesOperation
            {
                PrincipalName = "app_role_user",
                SchemaName = "app",
                Privileges = ["USAGE"]
            },
            new GrantSchemaOrTablePrivilegesOperation
            {
                PrincipalName = "app_role_user",
                SchemaName = "app",
                TableName = "orders",
                Privileges = ["SELECT", "UPDATE"]
            },
            new CreateSchemaWithOwnerOperation
            {
                SchemaName = "app",
                OwnerPrincipalName = "app_role_user"
            },
            new SeedIdempotentSqlOperation
            {
                SeedKey = "seed-security",
                Sql = "INSERT INTO app.\"security_claims\"(\"name\") VALUES ('admin');"
            },
            new SetDatabaseOptionOperation
            {
                OptionName = "work_mem",
                OptionValue = "64MB"
            },
            new SetIndexStorageParameterOperation
            {
                SchemaName = "app",
                TableName = "orders",
                IndexName = "ix_orders_created_on",
                ParameterName = "fillfactor",
                ParameterValue = "70"
            }
        };

        // Act
        var sql = GenerateSql(generator, operations);

        // Assert
        await Assert.That(sql).Contains("CREATE ROLE app_role_user LOGIN PASSWORD 'pg-secret'");
        await Assert.That(sql).Contains("GRANT pg_read_all_data TO app_role_user");
        await Assert.That(sql).Contains("GRANT USAGE ON SCHEMA app TO app_role_user");
        await Assert.That(sql).Contains("GRANT SELECT, UPDATE ON TABLE app.orders TO app_role_user");
        await Assert.That(sql).Contains("CREATE SCHEMA app AUTHORIZATION app_role_user");
        await Assert.That(sql).Contains("__EfCustomMigrationSeedHistory");
        await Assert.That(sql).Contains("ALTER DATABASE CURRENT SET work_mem = '64MB'");
        await Assert.That(sql).Contains("ALTER INDEX app.ix_orders_created_on SET (fillfactor = 70)");
    }

    [Test]
    public async Task GeneratePostgreSql_WithUnsupportedDatabaseOption_ThrowsInvalidOperationException()
    {
        // Arrange
        using var dbContext = CreatePostgreSqlDbContext();
        var generator = dbContext.GetService<IMigrationsSqlGenerator>();
        var operation = new SetDatabaseOptionOperation
        {
            OptionName = "CompatibilityLevel",
            OptionValue = "160"
        };

        // Act / Assert
        await Assert.That(() => GenerateSql(generator, [operation])).Throws<InvalidOperationException>();
    }

    private static TestSqlServerDbContext CreateSqlServerDbContext()
    {
        var options = new DbContextOptionsBuilder<TestSqlServerDbContext>()
            .UseSqlServer("Server=example.invalid;Database=efcustommigrations;User Id=placeholder;Password=placeholder;Encrypt=False")
            .UseEfCustomMigrationOperationsForSqlServer()
            .Options;

        return new TestSqlServerDbContext(options);
    }

    private static TestPostgreSqlDbContext CreatePostgreSqlDbContext()
    {
        var options = new DbContextOptionsBuilder<TestPostgreSqlDbContext>()
            .UseNpgsql("Host=example.invalid;Database=efcustommigrations;Username=placeholder;Password=placeholder")
            .UseEfCustomMigrationOperationsForPostgreSql()
            .Options;

        return new TestPostgreSqlDbContext(options);
    }

    private static string GenerateSql(IMigrationsSqlGenerator generator, IReadOnlyList<MigrationOperation> operations)
    {
        var commands = generator.Generate(operations, model: null);
        return string.Join(Environment.NewLine, commands.Select(command => command.CommandText));
    }

    private sealed class TestSqlServerDbContext(DbContextOptions<TestSqlServerDbContext> options) : DbContext(options);

    private sealed class TestPostgreSqlDbContext(DbContextOptions<TestPostgreSqlDbContext> options) : DbContext(options);
}
