using System.Text.Json.Serialization;

namespace WorldsOfTheNextRealm.AuthenticationService.Entities;

public record SigningKeyData(
    [property: JsonPropertyName("kid")] string Kid,
    [property: JsonPropertyName("alg")] string Algorithm,
    [property: JsonPropertyName("pub")] string PublicKey,
    [property: JsonPropertyName("epk")] string EncryptedPrivateKey,
    [property: JsonPropertyName("st")] string Status,
    [property: JsonPropertyName("ca")] long CreatedAt,
    [property: JsonPropertyName("ra")] long RotatedAt,
    [property: JsonPropertyName("rta")] long RetiredAt);
