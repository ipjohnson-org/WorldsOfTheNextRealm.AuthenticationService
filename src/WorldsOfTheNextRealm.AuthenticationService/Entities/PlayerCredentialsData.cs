using System.Text.Json.Serialization;

namespace WorldsOfTheNextRealm.AuthenticationService.Entities;

public record PlayerCredentialsData(
    [property: JsonPropertyName("pid")] string PlayerId,
    [property: JsonPropertyName("ph")] string PasswordHash,
    [property: JsonPropertyName("st")] string Status,
    [property: JsonPropertyName("fa")] int FailedAttempts,
    [property: JsonPropertyName("lu")] long LockUntil,
    [property: JsonPropertyName("ca")] long CreatedAt);
