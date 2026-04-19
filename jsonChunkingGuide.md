# JSON chunking guide

The `jsonstore` library stores JSON payloads as ordered chunks. Choose chunk sizes by payload range:

- **< 1 MB**: use **64 KB** chunks
- **< 10 MB**: use **256 KB** chunks
- **< 100 MB**: use **1 MB** chunks
- **>= 100 MB**: use **4 MB** chunks

## Why these sizes

- Smaller payloads benefit from smaller chunks to avoid over-fetching.
- Mid-sized payloads reduce row count with 256 KB chunks while keeping memory moderate.
- Large payloads (up to 100 MB) perform well with 1 MB chunks and lower index overhead.
- Very large payloads benefit from 4 MB chunks to keep chunk table growth manageable.

## Suggested schema/indexing

Current implementation uses:

- `json_entries` table: one row per key (`Id`, `Key`, `KeyHash`, `JsonType`, `TotalBytes`, `ChunkCount`)
- `json_chunks` table: many rows per payload (`JsonId`, `ChunkIndex`, `Data`)

Recommended indexes:

- Unique index on `json_entries.KeyHash`
- Unique composite index on `json_chunks(JsonId, ChunkIndex)`

If your workload differs strongly by payload size, you can partition or split chunk tables by size bands (<1 MB, <10 MB, <100 MB, >=100 MB), but the default two-table model is usually sufficient.
