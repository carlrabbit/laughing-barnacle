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

    public async Task<KvReadResult?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        KeyValidation.Validate(key);

        var existing = await dbContext.Entries.AsNoTracking().SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        return new KvReadResult(existing.Key, existing.VersionId, existing.Kind, existing.ValueBytes);
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

        if (string.IsNullOrEmpty(expectedVersionId))
        {
            var versionId = Guid.NewGuid().ToString("N");
            dbContext.Entries.Add(
                new KvEntryRecord
                {
                    Key = key,
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
                return new VersionedUpsertResult(false, false, null);
            }
        }

        var existing = await dbContext.Entries.SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
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
