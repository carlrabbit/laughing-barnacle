using System.Text;

namespace Binstore;
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
        if (Encoding.UTF8.GetByteCount(key) > BinStoreDbContext.MaxKeyBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(key), "Key must be 4KB or smaller when UTF-8 encoded.");
        }
    }
}
