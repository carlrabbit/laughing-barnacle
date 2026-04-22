namespace Kvstore;

/// <summary>
/// Documentation.
/// </summary>
public sealed class KvEntryRecord
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public required string Key { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public required string KeyHash { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public required string VersionId { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public KvEntryKind Kind { get; set; }
    /// <summary>
    /// Documentation.
    /// </summary>
    public required byte[] ValueBytes { get; set; }
}
