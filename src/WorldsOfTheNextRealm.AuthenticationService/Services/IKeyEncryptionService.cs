namespace WorldsOfTheNextRealm.AuthenticationService.Services;

public interface IKeyEncryptionService
{
    string Encrypt(byte[] plaintext);
    byte[] Decrypt(string ciphertext);
}
