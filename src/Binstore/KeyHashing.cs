using System.Security.Cryptography;
using System.Text;

namespace Binstore;

internal static class KeyHashing
{
    public static string Compute(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
}
