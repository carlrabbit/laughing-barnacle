using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Kvstore;

/// <summary>
/// Documentation.
/// </summary>
public sealed class WriteOnceKeyValueStore(KvStoreDbContext dbContext) : IWriteOnceKeyValueStore
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<WriteOnceStoreResult> StoreStringAsync(
        string key,
        string value,
        CancellationToken cancellationToken = default) =>
        StoreCoreAsync(key, KvEntryKind.String, Encoding.UTF8.GetBytes(value), cancellationToken);

    /// <summary>
    /// Documentation.
    /// </summary>
    public Task<WriteOnceStoreResult> StoreBlobAsync(
        string key,
        byte[] value,
        CancellationToken cancellationToken = default) =>
        StoreCoreAsync(key, KvEntryKind.Blob, value, cancellationToken);

    /// <inheritdoc />
    public async Task<WriteOnceStoreResult> StoreBlobAsync(
        string key,
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
        return await StoreCoreAsync(key, KvEntryKind.Blob, memoryStream.ToArray(), cancellationToken);
    }

    /// <summary>
    /// Documentation.
    /// </summary>
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

    private async Task<WriteOnceStoreResult> StoreCoreAsync(
        string key,
        KvEntryKind kind,
        byte[] valueBytes,
        CancellationToken cancellationToken)
    {
        KeyValidation.Validate(key);
        ArgumentNullException.ThrowIfNull(valueBytes);
        var keyHash = KeyHashing.Compute(key);

        var existing = await dbContext.Entries
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.KeyHash == keyHash && x.Key == key, cancellationToken);
        if (existing is not null)
        {
            return new WriteOnceStoreResult(false, existing.VersionId);
        }

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
        }
        catch (DbUpdateException)
        {
            var current = await dbContext.Entries
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.KeyHash == keyHash && x.Key == key, cancellationToken);
            if (current is not null)
            {
                return new WriteOnceStoreResult(false, current.VersionId);
            }

            throw;
        }

        return new WriteOnceStoreResult(true, versionId);
    }
}
