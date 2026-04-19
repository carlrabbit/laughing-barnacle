using System.Buffers;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Jsonstore;

public sealed class WriteOnceJsonStore(JsonStoreDbContext dbContext) : IWriteOnceJsonStore
{
    public Task<JsonStoreResult> StoreAsync(string key, string json, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(json);
        return StoreAsync(key, new MemoryStream(Encoding.UTF8.GetBytes(json), writable: false), cancellationToken);
    }

    public async Task<JsonStoreResult> StoreAsync(
        string key,
        Stream jsonStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonStream);
        if (!jsonStream.CanRead)
        {
            throw new ArgumentException("JSON stream must be readable.", nameof(jsonStream));
        }

        KeyValidation.Validate(key);
        var keyHash = KeyHashing.Compute(key);

        var existing = await dbContext.Jsons
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.KeyHash == keyHash && x.Key == key, cancellationToken);
        if (existing is not null)
        {
            return new JsonStoreResult(false, existing.Id, existing.TotalBytes, existing.ChunkCount);
        }

        var record = new JsonRecord
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

        dbContext.Jsons.Add(record);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            var chunkSize = ChunkSizing.GetChunkSize(jsonStream.CanSeek ? jsonStream.Length - jsonStream.Position : null);
            var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
            try
            {
                var chunkIndex = 0;
                long totalBytes = 0;
                int bytesRead;
                while ((bytesRead = await jsonStream.ReadAsync(buffer.AsMemory(0, chunkSize), cancellationToken)) > 0)
                {
                    dbContext.JsonChunks.Add(new JsonChunkRecord
                    {
                        JsonId = record.Id,
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
            var current = await dbContext.Jsons
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.KeyHash == keyHash && x.Key == key, cancellationToken);
            if (current is not null)
            {
                return new JsonStoreResult(false, current.Id, current.TotalBytes, current.ChunkCount);
            }

            throw;
        }

        return new JsonStoreResult(true, record.Id, record.TotalBytes, record.ChunkCount);
    }

    public async Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken = default)
    {
        KeyValidation.Validate(key);
        var keyHash = KeyHashing.Compute(key);

        var record = await dbContext.Jsons
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.KeyHash == keyHash && x.Key == key, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var chunkEnumerator = dbContext.JsonChunks
            .AsNoTracking()
            .Where(x => x.JsonId == record.Id)
            .OrderBy(x => x.ChunkIndex)
            .Select(x => x.Data)
            .AsAsyncEnumerable()
            .GetAsyncEnumerator(cancellationToken);

        return new ChunkedReadStream(chunkEnumerator);
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        using var stream = await GetStreamAsync(key, cancellationToken);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
