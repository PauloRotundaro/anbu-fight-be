using System.Globalization;
using System.Security.Cryptography;

namespace AnbuFight.Infrastructure.Identity;

/// <summary>
/// PBKDF2-HMAC-SHA256, the algorithm ASP.NET Core Identity itself uses. The iteration count is
/// stored with the hash, so it can be raised later without invalidating existing passwords.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const char Separator = '.';

    public string DecoyHash { get; } = Format(Iterations, new byte[SaltSize], new byte[KeySize]);

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Derive(password, salt, Iterations);

        return Format(Iterations, salt, key);
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split(Separator);

        if (parts.Length != 3 ||
            !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expectedKey = Convert.FromBase64String(parts[2]);
            var actualKey = Derive(password, salt, iterations);

            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Derive(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, KeySize);

    private static string Format(int iterations, byte[] salt, byte[] key) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{iterations}{Separator}{Convert.ToBase64String(salt)}{Separator}{Convert.ToBase64String(key)}");
}
