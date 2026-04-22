namespace Kvstore;
/// <summary>
/// Represents kv entry record.
/// </summary>

public sealed class KvEntryRecord
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    public required string Key { get; set; }
    /// <summary>
    /// Gets or sets the key hash.
    /// </summary>
    public required string KeyHash { get; set; }
    /// <summary>
    /// Gets or sets the version id.
    /// </summary>
    public required string VersionId { get; set; }
    /// <summary>
    /// Gets or sets the kind.
    /// </summary>
    public KvEntryKind Kind { get; set; }
    /// <summary>
    /// Gets or sets the value bytes.
    /// </summary>
    public required byte[] ValueBytes { get; set; }
}
