using System.Text;

namespace Binstore;

internal static class KeyValidation
{
    public static void Validate(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (Encoding.UTF8.GetByteCount(key) > BinStoreDbContext.MaxKeyBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(key), "Key must be 4KB or smaller when UTF-8 encoded.");
        }
    }
}
