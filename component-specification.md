# Component specification

## How to add a new component description

When adding a new component, append a section using this structure:

1. **Component name**
2. **Purpose** - what problem it solves in this repository.
3. **Projects** - source and test project paths.
4. **Public API** - key public types and extension points.
5. **Storage and data model** - persisted entities/tables or state shape (if any).
6. **Dependencies** - external packages/framework features the component relies on.
7. **Operational notes** - constraints, validation rules, performance or security details.

## Current components

### Statistics

- **Purpose:** lock-free online descriptive statistics for streaming numeric samples.
- **Projects:** `src/Statistics`, `tests/Statistics.Tests`.
- **Public API:** `OnlineDescriptiveStatistics`, `StatisticsSnapshot`.
- **Operational notes:** supports consistent multi-metric reads via snapshots and rejects non-finite values.

### Binstore

- **Purpose:** write-once binary blob storage with chunked persistence and retrieval.
- **Projects:** `src/Binstore`, `tests/Binstore.Tests`.
- **Public API:** `IWriteOnceBinaryStore`, `WriteOnceBinaryStore`, `BinStoreResult`.
- **Storage and data model:** `BinRecord` and `BinChunkRecord` mapped by `BinStoreDbContext`.

### Jsonstore

- **Purpose:** write-once JSON storage with root-type detection and streamed object/array reads.
- **Projects:** `src/Jsonstore`, `tests/Jsonstore.Tests`.
- **Public API:** `IWriteOnceJsonStore`, `WriteOnceJsonStore`, `JsonStoreResult`, `JsonRootType`.
- **Storage and data model:** `JsonRecord` and `JsonChunkRecord` mapped by `JsonStoreDbContext`.

### EFcore migrations

- **Purpose:** provider-specific EF Core custom migration operations and SQL generation.
- **Projects:** `src/EfCustomMigrations`, `tests/EfCustomMigrations.Tests`.
- **Public API:** `MigrationBuilderExtensions`, `DbContextOptionsBuilderExtensions`, custom `MigrationOperation` types.
- **Operational notes:** includes SQL Server and PostgreSQL generators and password resolution support.
