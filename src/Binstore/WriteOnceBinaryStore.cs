using System.Buffers;
using Microsoft.EntityFrameworkCore;

namespace Binstore;

public sealed class WriteOnceBinaryStore(BinStoreDbContext dbContext) : IWriteOnceBinaryStore
{
    public Task<BinStoreResult> StoreAsync(string key, byte[] value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        return StoreAsync(key, new MemoryStream(value, writable: false), cancellationToken);
    }

    public async Task<BinStoreResult> StoreAsync(
        string key,
        Stream valueStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(valueStream);
        if (!valueStream.CanRead)
        {
            throw new ArgumentException("Blob stream must be readable.", nameof(valueStream));
        }

        KeyValidation.Validate(key);
        var keyHash = KeyHashing.Compute(key);

        var existing = await dbContext.Bins
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.KeyHash == keyHash && x.Key == key, cancellationToken);
        if (existing is not null)
        {
            return new BinStoreResult(false, existing.Id, existing.TotalBytes, existing.ChunkCount);
        }

        var record = new BinRecord
        {
            Key = key,
            KeyHash = keyHash,
            CreatedUtc = DateTimeOffset.UtcNow,
            TotalBytes = 0,
            ChunkCount = 0
        };

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        dbContext.Bins.Add(record);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            var chunkSize = ChunkSizing.GetChunkSize(valueStream.CanSeek ? valueStream.Length - valueStream.Position : null);
            var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
            try
            {
                var chunkIndex = 0;
                long totalBytes = 0;
                int bytesRead;
                while ((bytesRead = await valueStream.ReadAsync(buffer.AsMemory(0, chunkSize), cancellationToken)) > 0)
                {
                    dbContext.Chunks.Add(new BinChunkRecord
                    {
                        BinId = record.Id,
                        ChunkIndex = chunkIndex,
                        Data = buffer[..bytesRead].ToArray()
                    });
                    chunkIndex++;
                    totalBytes += bytesRead;
                }

                record.TotalBytes = totalBytes;
                record.ChunkCount = chunkIndex;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateException)
        {
            var current = await dbContext.Bins
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.KeyHash == keyHash && x.Key == key, cancellationToken);
            if (current is not null)
            {
                return new BinStoreResult(false, current.Id, current.TotalBytes, current.ChunkCount);
            }

            throw;
        }

        return new BinStoreResult(true, record.Id, record.TotalBytes, record.ChunkCount);
    }

    public async Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken = default)
    {
        KeyValidation.Validate(key);
        var keyHash = KeyHashing.Compute(key);

        var record = await dbContext.Bins
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.KeyHash == keyHash && x.Key == key, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var chunks = await dbContext.Chunks
            .AsNoTracking()
            .Where(x => x.BinId == record.Id)
            .OrderBy(x => x.ChunkIndex)
            .Select(x => x.Data)
            .ToListAsync(cancellationToken);

        return new ChunkedReadStream(chunks);
    }
}
