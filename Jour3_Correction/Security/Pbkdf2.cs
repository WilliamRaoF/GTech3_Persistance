using System.Security.Cryptography;

namespace Game.Persistence.Mongo.Security;

public static class Pbkdf2
{
    public const int SaltSize = 16;     // 128 bits
    public const int HashSize = 32;     // 256 bits
    public const int DefaultIterations = 100_000;

    public static (string hashB64, string saltB64, int iterations) HashPassword(string password, int? iterations = null)
    {
        var it = iterations ?? DefaultIterations;
        Span<byte> salt = stackalloc byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, it, HashAlgorithmName.SHA256, HashSize);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt), it);
    }

    public static bool Verify(string password, string storedHashB64, string storedSaltB64, int iterations)
    {
        var salt = Convert.FromBase64String(storedSaltB64);
        var expected = Convert.FromBase64String(storedHashB64);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        // Comparaison constante
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
