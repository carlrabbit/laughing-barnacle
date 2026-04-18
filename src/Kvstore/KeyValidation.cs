using System.Text;

namespace Kvstore;

internal static class KeyValidation
{
    public static void Validate(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (Encoding.UTF8.GetByteCount(key) > KvStoreDbContext.MaxKeyBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(key), "Key must be 10MB or smaller when UTF-8 encoded.");
        }
    }
}
