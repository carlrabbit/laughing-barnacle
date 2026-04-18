# Chunking Guide

The `binstore` library stores binary payloads as ordered chunks. Choose chunk sizes by payload range:

- **< 1 MB**: use **64 KB** chunks
- **< 10 MB**: use **256 KB** chunks
- **< 100 MB**: use **1 MB** chunks
- **>= 100 MB**: use **4 MB** chunks

## Why these sizes

- Smaller files benefit from smaller chunks to avoid over-fetching.
- Mid-sized files reduce row count with 256 KB chunks while keeping memory moderate.
- Large files (up to 100 MB) perform well with 1 MB chunks and lower index overhead.
- Very large files benefit from 4 MB chunks to keep chunk table growth manageable.

## Suggested schema/indexing

Current implementation uses:

- `bin_entries` table: one row per key (`Id`, `Key`, `KeyHash`, `TotalBytes`, `ChunkCount`)
- `bin_chunks` table: many rows per payload (`BinId`, `ChunkIndex`, `Data`)

Recommended indexes:

- Unique index on `bin_entries.KeyHash`
- Unique composite index on `bin_chunks(BinId, ChunkIndex)`

If your workload differs strongly by payload size, you can partition or split chunk tables by size bands (<1 MB, <10 MB, <100 MB, >=100 MB), but the default two-table model is usually sufficient.
