using System.Security.Cryptography;
using DependencyModules.Runtime.Attributes;
using WorldsOfTheNextRealm.AuthenticationService.Configuration;

namespace WorldsOfTheNextRealm.AuthenticationService.Services;

[SingletonService]
public class KeyEncryptionService(AuthSettings settings) : IKeyEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public string Encrypt(byte[] plaintext)
    {
        var key = Convert.FromBase64String(settings.MasterEncryptionKey);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return $"{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(tag)}:{Convert.ToBase64String(ciphertext)}";
    }

    public byte[] Decrypt(string encoded)
    {
        var parts = encoded.Split(':');
        if (parts.Length != 3)
        {
            throw new CryptographicException("Invalid encrypted data format.");
        }

        var key = Convert.FromBase64String(settings.MasterEncryptionKey);
        var nonce = Convert.FromBase64String(parts[0]);
        var tag = Convert.FromBase64String(parts[1]);
        var ciphertext = Convert.FromBase64String(parts[2]);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }
}
