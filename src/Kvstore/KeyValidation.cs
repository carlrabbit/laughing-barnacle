using System.Text;

namespace Kvstore;
/// <summary>
/// Represents key validation.
/// </summary>

internal static class KeyValidation
{
    /// <summary>
    /// Performs the validate operation.
    /// </summary>
    /// <param name="key">The key.</param>
    public static void Validate(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (Encoding.UTF8.GetByteCount(key) > KvStoreDbContext.MaxKeyBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(key), "Key must be 10MB or smaller when UTF-8 encoded.");
        }
    }
}
