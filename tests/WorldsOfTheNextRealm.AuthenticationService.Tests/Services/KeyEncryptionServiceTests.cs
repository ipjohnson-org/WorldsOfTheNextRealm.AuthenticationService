using DependencyModules.xUnit.Attributes;
using WorldsOfTheNextRealm.AuthenticationService.Services;
using WorldsOfTheNextRealm.AuthenticationService.Tests.TestHelpers;

namespace WorldsOfTheNextRealm.AuthenticationService.Tests.Services;

public class KeyEncryptionServiceTests
{
    [ModuleTest]
    [TestAuthSettings]
    public void Encrypt_Decrypt_RoundTrip_Succeeds(IKeyEncryptionService service)
    {
        var plaintext = System.Text.Encoding.UTF8.GetBytes("This is secret key material");

        var encrypted = service.Encrypt(plaintext);
        var decrypted = service.Decrypt(encrypted);

        Assert.Equal(plaintext, decrypted);
    }

    [ModuleTest]
    [TestAuthSettings]
    public void Encrypt_ProducesDifferentCiphertexts(IKeyEncryptionService service)
    {
        var plaintext = System.Text.Encoding.UTF8.GetBytes("Same plaintext");

        var encrypted1 = service.Encrypt(plaintext);
        var encrypted2 = service.Encrypt(plaintext);

        Assert.NotEqual(encrypted1, encrypted2);
    }

    [ModuleTest]
    [TestAuthSettings]
    public void Encrypt_OutputFormat_HasThreeParts(IKeyEncryptionService service)
    {
        var plaintext = System.Text.Encoding.UTF8.GetBytes("test");

        var encrypted = service.Encrypt(plaintext);
        var parts = encrypted.Split(':');

        Assert.Equal(3, parts.Length);
    }

    [ModuleTest]
    [TestAuthSettings]
    public void Decrypt_TamperedCiphertext_Throws(IKeyEncryptionService service)
    {
        var plaintext = System.Text.Encoding.UTF8.GetBytes("test data");
        var encrypted = service.Encrypt(plaintext);

        var parts = encrypted.Split(':');
        // Tamper with the ciphertext
        var tamperedBytes = Convert.FromBase64String(parts[2]);
        tamperedBytes[0] ^= 0xFF;
        parts[2] = Convert.ToBase64String(tamperedBytes);
        var tampered = string.Join(':', parts);

        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(
            () => service.Decrypt(tampered));
    }
}
