using System.Security.Cryptography;
using System.Text;

namespace MedMateAI.Infrastructure.Auth.Security;

internal static class RefreshTokenHasher
{
    public static string Sha256Hex(string plain)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToHexString(hash);
    }
}
