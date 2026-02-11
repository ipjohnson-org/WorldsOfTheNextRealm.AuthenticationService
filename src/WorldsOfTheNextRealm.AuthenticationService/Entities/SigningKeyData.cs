namespace WorldsOfTheNextRealm.AuthenticationService.Entities;

public record SigningKeyData(
    string Kid,
    string Algorithm,
    string PublicKey,
    string EncryptedPrivateKey,
    string Status,
    long CreatedAt,
    long RotatedAt,
    long RetiredAt);
