using System.Security.Cryptography;

namespace PingPongStats.Core.Services;

/// <summary>
/// Hashes and verifies the optional 4-digit player login PIN using PBKDF2. This is
/// convenience, not real security: anyone with file access to the XML data
/// directory can read/edit it directly, and the PIN only exists to avoid one
/// player accidentally opening another's profile. See README for the disclaimer.
/// </summary>
public static class PinService
{
    private const int Iterations = 100_000;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;

    public static bool IsValidPinFormat(string? pin) =>
        pin is not null && pin.Length == 4 && pin.All(char.IsDigit);

    public static (string Hash, string Salt) HashPin(string pin)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(pin, saltBytes, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public static bool VerifyPin(string pin, string storedHash, string storedSalt)
    {
        if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt)) return false;

        var saltBytes = Convert.FromBase64String(storedSalt);
        var expectedBytes = Convert.FromBase64String(storedHash);
        var actualBytes = Rfc2898DeriveBytes.Pbkdf2(pin, saltBytes, Iterations, HashAlgorithmName.SHA256, expectedBytes.Length);

        return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
