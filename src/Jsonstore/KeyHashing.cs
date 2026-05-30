using System.Security.Cryptography;
using System.Text;

namespace Jsonstore;
/// <summary>
/// Represents key hashing.
/// </summary>

internal static class KeyHashing
{
    /// <summary>
    /// Performs the compute operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public static string Compute(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
}
