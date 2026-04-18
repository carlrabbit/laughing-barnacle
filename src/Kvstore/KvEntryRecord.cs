namespace Kvstore;

public sealed class KvEntryRecord
{
    public long Id { get; set; }
    public required string Key { get; set; }
    public required string KeyHash { get; set; }
    public required string VersionId { get; set; }
    public KvEntryKind Kind { get; set; }
    public required byte[] ValueBytes { get; set; }
}
