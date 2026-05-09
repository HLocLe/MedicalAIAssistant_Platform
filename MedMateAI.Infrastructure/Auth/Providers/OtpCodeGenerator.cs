using System.Security.Cryptography;

namespace MedMateAI.Infrastructure.Auth.Providers;

public static class OtpCodeGenerator
{
    public static string CreateNumeric(int digitCount = 6)
    {
        if (digitCount is < 4 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(digitCount), "digitCount must be between 4 and 10.");
        }

        var maxExclusive = (int)Math.Pow(10, digitCount);
        return RandomNumberGenerator.GetInt32(0, maxExclusive).ToString($"D{digitCount}");
    }
}
