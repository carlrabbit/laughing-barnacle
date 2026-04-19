# EF Custom Migration Operations

This library provides custom Entity Framework Core migration operations for:

- Microsoft SQL Server (tested with SQL Server 2022)
- PostgreSQL

The first included operation is `CreateSqlUser`, implemented as a custom migration operation with provider-specific SQL generation.

## Setup

1. Add a reference to the `EfCustomMigrations` project/package.
2. Register the provider-specific SQL generator replacement in your `DbContext` options:

```csharp
options.UseSqlServer(connectionString)
    .UseEfCustomMigrationOperationsForSqlServer();
```

or

```csharp
options.UseNpgsql(connectionString)
    .UseEfCustomMigrationOperationsForPostgreSql();
```

## Using `CreateSqlUser` in a migration

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateSqlUser(
        userName: "app_user",
        passwordEnvironmentVariable: "APP_DB_USER_PASSWORD");
}
```

You can also provide a direct password:

```csharp
migrationBuilder.CreateSqlUser(
    userName: "app_user",
    password: "replace-with-secure-value");
```

### Password resolution behavior

- If `password` is provided, it is used.
- Otherwise, `passwordEnvironmentVariable` is used to read the password from the process environment.
- If neither source is available, SQL generation throws.

## Warnings and best practices

- Prefer environment variables over hardcoded secrets in migrations.
- Avoid checking real credentials into source control.
- Limit user permissions after creation (least privilege).
- Rotate credentials after initial provisioning.
- Run migrations in controlled environments where required environment variables are explicitly set.

## Other helpful custom migration operations for full database bootstrap

- Create login/role with scoped permissions
- Grant schema/table-level privileges
- Create schemas and set ownership
- Enable provider-specific extensions (e.g., PostgreSQL extensions)
- Seed reference/security data with idempotent SQL
- Create and manage database-level settings (collation, compatibility options)
- Add provider-specific indexes or storage parameters not modeled directly by EF Core
