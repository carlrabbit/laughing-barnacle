# EF Custom Migration Operations

This library provides custom Entity Framework Core migration operations for:

- Microsoft SQL Server (tested with SQL Server 2022)
- PostgreSQL

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

## Included operations

- `CreateSqlUser`
- `CreateSqlLoginOrRole`
- `GrantSchemaOrTablePrivileges`
- `CreateSchemaWithOwner`
- `SeedIdempotentSql`
- `SetDatabaseOption`
- `SetIndexStorageParameter`

## `CreateSqlUser` usage

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateSqlUser(
        userName: "app_user",
        passwordEnvironmentVariable: "APP_DB_USER_PASSWORD");
}
```

You can also pass `password` directly. If both are set, `password` is used.

### Password resolution rules

- Use `password` when provided.
- Otherwise read from `passwordEnvironmentVariable`.
- Throw during SQL generation if no usable password source is available.

## Operations requested for full bootstrap

### 1) Create login/role with scoped permissions

Use `CreateSqlLoginOrRole` with role memberships:

```csharp
migrationBuilder.CreateSqlLoginOrRole(
    principalName: "app_role_user",
    passwordEnvironmentVariable: "APP_ROLE_PASSWORD",
    defaultSchema: "app",
    memberOfRoles: ["db_datareader", "db_datawriter"]);
```

- SQL Server: creates login + mapped database user + optional default schema + role memberships.
- PostgreSQL: creates role with `LOGIN` + role membership grants.

### 2) Grant schema/table-level privileges

```csharp
migrationBuilder.GrantSchemaOrTablePrivileges(
    principalName: "app_role_user",
    schemaName: "app",
    privileges: ["SELECT", "UPDATE"],
    tableName: "orders");
```

- Schema-only grants are also supported (`tableName: null`).

### 3) Create schemas and set ownership

```csharp
migrationBuilder.CreateSchemaWithOwner(
    schemaName: "app",
    ownerPrincipalName: "app_role_user");
```

- Emits provider-specific create-if-missing and ownership statements.

### 4) Seed reference/security data with idempotent SQL

```csharp
migrationBuilder.SeedIdempotentSql(
    seedKey: "seed-security-claims-v1",
    sql: "INSERT INTO app.security_claims(name) VALUES ('admin');");
```

- Uses `__EfCustomMigrationSeedHistory` to ensure each `seedKey` executes once.
- Works well for immutable reference data, claims, default roles, and bootstrap permissions.

### 5) Create/manage database-level settings

```csharp
// SQL Server examples
migrationBuilder.SetDatabaseOption("CompatibilityLevel", "160");
migrationBuilder.SetDatabaseOption("Collation", "Latin1_General_100_CI_AS_SC");

// PostgreSQL example
migrationBuilder.SetDatabaseOption("work_mem", "64MB");
```

- SQL Server supports compatibility/collation plus generic `ALTER DATABASE ... SET`.
- PostgreSQL supports `ALTER DATABASE CURRENT SET ...`.
- PostgreSQL does **not** support SQL Server compatibility level semantics; unsupported options throw.

## Separate chapter: common provider-specific index/storage tuning

Some high-impact tuning options are provider specific and not always modeled the same way in EF Core.
Use `SetIndexStorageParameter` for these cases.

### SQL Server: common options

```csharp
migrationBuilder.SetIndexStorageParameter(
    schemaName: "app",
    tableName: "orders",
    indexName: "IX_orders_created_on",
    parameterName: "FILLFACTOR",
    parameterValue: "80");
```

Common SQL Server parameters:

- `FILLFACTOR` (e.g. `80`) to reduce page-split pressure on frequently updated indexes.
- `ONLINE = ON` (Enterprise/edition-dependent) for reduced lock impact during rebuilds.
- `SORT_IN_TEMPDB = ON` to shift sort workload to `tempdb` during rebuild.
- `PAD_INDEX = ON` to apply fillfactor to intermediate index pages.

### PostgreSQL: common options

```csharp
migrationBuilder.SetIndexStorageParameter(
    schemaName: "app",
    tableName: "orders",
    indexName: "ix_orders_created_on",
    parameterName: "fillfactor",
    parameterValue: "70");
```

Common PostgreSQL parameters:

- `fillfactor` to reserve free space per index page and reduce split churn.
- `deduplicate_items` (B-tree) to reduce duplicate-value footprint.
- `fastupdate` (GIN) to optimize write behavior by buffering pending entries.

## Warnings and best practices

- Prefer environment variables over hardcoded credentials.
- Keep migration SQL idempotent and deterministic.
- Grant least privilege first, then add only required rights.
- Use separate principals for app runtime, read-only jobs, and migration runners.
- Validate provider-specific options in pre-production before applying at scale.
