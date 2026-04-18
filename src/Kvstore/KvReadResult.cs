using System.Text;

namespace Kvstore;

public sealed record KvReadResult(string Key, string VersionId, KvEntryKind Kind, byte[] ValueBytes)
{
    public string AsString() => Encoding.UTF8.GetString(ValueBytes);
}
