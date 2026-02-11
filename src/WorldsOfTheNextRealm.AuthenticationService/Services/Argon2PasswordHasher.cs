using System.Security.Cryptography;
using DependencyModules.Runtime.Attributes;
using Konscious.Security.Cryptography;

namespace WorldsOfTheNextRealm.AuthenticationService.Services;

[SingletonService]
public class Argon2PasswordHasher : IPasswordHasher
{
    private const int MemorySize = 65536; // 64 MB
    private const int Iterations = 3;
    private const int Parallelism = 1;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public async Task<string> Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = MemorySize,
            Iterations = Iterations,
            DegreeOfParallelism = Parallelism
        };

        var hash = await argon2.GetBytesAsync(HashSize);

        var saltBase64 = Convert.ToBase64String(salt);
        var hashBase64 = Convert.ToBase64String(hash);

        return $"$argon2id$v=19$m={MemorySize},t={Iterations},p={Parallelism}${saltBase64}${hashBase64}";
    }

    public async Task<bool> Verify(string password, string encodedHash)
    {
        var parts = encodedHash.Split('$');
        // Format: $argon2id$v=19$m=65536,t=3,p=1${salt}${hash}
        if (parts.Length != 6 || parts[1] != "argon2id")
        {
            return false;
        }

        var paramParts = parts[3].Split(',');
        var memory = int.Parse(paramParts[0][2..]); // m=
        var iterations = int.Parse(paramParts[1][2..]); // t=
        var parallelism = int.Parse(paramParts[2][2..]); // p=

        var salt = Convert.FromBase64String(parts[4]);
        var expectedHash = Convert.FromBase64String(parts[5]);

        var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memory,
            Iterations = iterations,
            DegreeOfParallelism = parallelism
        };

        var computedHash = await argon2.GetBytesAsync(expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
    }
}
