using System.Text;

namespace Kvstore;
/// <summary>
/// Represents kv read result.
/// </summary>

public sealed record KvReadResult(string Key, string VersionId, KvEntryKind Kind, byte[] ValueBytes)
{
    /// <summary>
    /// Performs the as string operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public string AsString() => Encoding.UTF8.GetString(ValueBytes);
}
