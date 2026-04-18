using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Kvstore;

public sealed class WriteOnceKeyValueStore(KvStoreDbContext dbContext) : IWriteOnceKeyValueStore
{
    public Task<WriteOnceStoreResult> StoreStringAsync(
        string key,
        string value,
        CancellationToken cancellationToken = default) =>
        StoreCoreAsync(key, KvEntryKind.String, Encoding.UTF8.GetBytes(value), cancellationToken);

    public Task<WriteOnceStoreResult> StoreBlobAsync(
        string key,
        byte[] value,
        CancellationToken cancellationToken = default) =>
        StoreCoreAsync(key, KvEntryKind.Blob, value, cancellationToken);

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

    private async Task<WriteOnceStoreResult> StoreCoreAsync(
        string key,
        KvEntryKind kind,
        byte[] valueBytes,
        CancellationToken cancellationToken)
    {
        KeyValidation.Validate(key);
        ArgumentNullException.ThrowIfNull(valueBytes);

        var existing = await dbContext.Entries.AsNoTracking().SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (existing is not null)
        {
            return new WriteOnceStoreResult(false, existing.VersionId);
        }

        var versionId = Guid.NewGuid().ToString("N");
        dbContext.Entries.Add(
            new KvEntryRecord
            {
                Key = key,
                Kind = kind,
                ValueBytes = valueBytes,
                VersionId = versionId
            });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new WriteOnceStoreResult(true, versionId);
    }
}
