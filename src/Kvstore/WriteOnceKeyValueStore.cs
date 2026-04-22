using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Kvstore;
/// <summary>
/// Represents write once key value store.
/// </summary>

public sealed class WriteOnceKeyValueStore(KvStoreDbContext dbContext) : IWriteOnceKeyValueStore
{
    /// <summary>
    /// Performs the store string async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public Task<WriteOnceStoreResult> StoreStringAsync(
        string key,
        string value,
        CancellationToken cancellationToken = default) =>
        StoreCoreAsync(key, KvEntryKind.String, Encoding.UTF8.GetBytes(value), cancellationToken);
    /// <summary>
    /// Performs the store blob async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

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
    /// Performs the get async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

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
