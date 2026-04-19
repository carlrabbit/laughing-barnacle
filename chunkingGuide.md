# JSON chunking guide

Recommended chunk sizes for write/read streaming in `jsonstore`:

- **< 1 MB JSON**: use **64 KB** chunks
  - Keeps chunk metadata low while still allowing responsive first-byte reads.
- **< 10 MB JSON**: use **256 KB** chunks
  - Good balance between chunk count and transaction overhead.
- **< 100 MB JSON**: use **1 MB** chunks
  - Reduces row count significantly for large payloads while keeping memory per chunk bounded.
- **> 100 MB JSON**: use **4 MB** chunks
  - Minimizes database row/index pressure for very large payloads.

These thresholds are implemented in `Jsonstore.ChunkSizing` and apply to both SQL Server and PostgreSQL providers.
