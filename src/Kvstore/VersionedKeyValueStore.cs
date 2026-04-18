using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Kvstore;

public sealed class VersionedKeyValueStore(KvStoreDbContext dbContext) : IVersionedKeyValueStore
{
    public Task<VersionedUpsertResult> UpsertStringAsync(
        string key,
        string? expectedVersionId,
        string value,
        CancellationToken cancellationToken = default) =>
        UpsertCoreAsync(key, expectedVersionId, KvEntryKind.String, Encoding.UTF8.GetBytes(value), cancellationToken);

    public Task<VersionedUpsertResult> UpsertBlobAsync(
        string key,
        string? expectedVersionId,
        byte[] value,
        CancellationToken cancellationToken = default) =>
        UpsertCoreAsync(key, expectedVersionId, KvEntryKind.Blob, value, cancellationToken);

    /// <inheritdoc />
    public async Task<VersionedUpsertResult> UpsertBlobAsync(
        string key,
        string? expectedVersionId,
        Stream valueStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(valueStream);
        if (!valueStream.CanRead)
        {
            throw new ArgumentException("Blob stream must be readable.", nameof(valueStream));
        }

        using var memoryStream = new MemoryStream();
        await valueStream.CopyToAsync(memoryStream, cancellationToken);
        return await UpsertCoreAsync(key, expectedVersionId, KvEntryKind.Blob, memoryStream.ToArray(), cancellationToken);
    }

    public async Task<KvReadResult?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        KeyValidation.Validate(key);
        var keyHash = KeyHashing.Compute(key);

        var existing = await dbContext.Entries
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.KeyHash == keyHash && x.Key == key, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        return new KvReadResult(existing.Key, existing.VersionId, existing.Kind, existing.ValueBytes);
    }

    /// <inheritdoc />
    public async Task<Stream?> GetBlobStreamAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await GetAsync(key, cancellationToken);
        if (value is null || value.Kind != KvEntryKind.Blob)
        {
            return null;
        }

        return new MemoryStream(value.ValueBytes, writable: false);
    }

    private async Task<VersionedUpsertResult> UpsertCoreAsync(
        string key,
        string? expectedVersionId,
        KvEntryKind kind,
        byte[] valueBytes,
        CancellationToken cancellationToken)
    {
        KeyValidation.Validate(key);
        ArgumentNullException.ThrowIfNull(valueBytes);
        var keyHash = KeyHashing.Compute(key);

        if (string.IsNullOrEmpty(expectedVersionId))
        {
            var versionId = Guid.NewGuid().ToString("N");
            dbContext.Entries.Add(
                new KvEntryRecord
                {
                    Key = key,
                    KeyHash = keyHash,
                    Kind = kind,
                    ValueBytes = valueBytes,
                    VersionId = versionId
                });

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return new VersionedUpsertResult(true, true, versionId);
            }
            catch (DbUpdateException)
            {
                var nowExists = await dbContext.Entries
                    .AsNoTracking()
                    .AnyAsync(x => x.KeyHash == keyHash && x.Key == key, cancellationToken);
                if (nowExists)
                {
                    return new VersionedUpsertResult(false, false, null);
                }

                throw;
            }
        }

        var existing = await dbContext.Entries
            .SingleOrDefaultAsync(x => x.KeyHash == keyHash && x.Key == key, cancellationToken);
        if (existing is null || existing.VersionId != expectedVersionId)
        {
            return new VersionedUpsertResult(false, false, null);
        }

        var newVersionId = Guid.NewGuid().ToString("N");
        existing.Kind = kind;
        existing.ValueBytes = valueBytes;
        existing.VersionId = newVersionId;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new VersionedUpsertResult(true, false, newVersionId);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new VersionedUpsertResult(false, false, null);
        }
    }
}
