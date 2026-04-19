using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

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
        var sql = GenerateSql(generator, operation);

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
            var sql = GenerateSql(generator, operation);

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
            await Assert.That(() => GenerateSql(generator, operation)).Throws<InvalidOperationException>();
        }
        finally
        {
            Environment.SetEnvironmentVariable(passwordVariable, originalValue);
        }
    }

    private static TestSqlServerDbContext CreateSqlServerDbContext()
    {
        var options = new DbContextOptionsBuilder<TestSqlServerDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=efcustommigrations;Trusted_Connection=True;Encrypt=False")
            .UseEfCustomMigrationOperationsForSqlServer()
            .Options;

        return new TestSqlServerDbContext(options);
    }

    private static TestPostgreSqlDbContext CreatePostgreSqlDbContext()
    {
        var options = new DbContextOptionsBuilder<TestPostgreSqlDbContext>()
            .UseNpgsql("Host=localhost;Database=efcustommigrations;Username=test;Password=test")
            .UseEfCustomMigrationOperationsForPostgreSql()
            .Options;

        return new TestPostgreSqlDbContext(options);
    }

    private static string GenerateSql(IMigrationsSqlGenerator generator, CreateSqlUserOperation operation)
    {
        var commands = generator.Generate([operation], model: null);
        return string.Join(Environment.NewLine, commands.Select(command => command.CommandText));
    }

    private sealed class TestSqlServerDbContext(DbContextOptions<TestSqlServerDbContext> options) : DbContext(options);

    private sealed class TestPostgreSqlDbContext(DbContextOptions<TestPostgreSqlDbContext> options) : DbContext(options);
}
