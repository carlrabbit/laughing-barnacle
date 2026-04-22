using System.Text;

namespace Kvstore;

/// <summary>
/// Documentation.
/// </summary>
public sealed record KvReadResult(string Key, string VersionId, KvEntryKind Kind, byte[] ValueBytes)
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public string AsString() => Encoding.UTF8.GetString(ValueBytes);
}
