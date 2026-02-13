using System.Text.Json.Serialization;

namespace WorldsOfTheNextRealm.AuthenticationService.Entities;

public record EmailLookupData(
    [property: JsonPropertyName("ne")] string NormalizedEmail,
    [property: JsonPropertyName("pid")] string PlayerId,
    [property: JsonPropertyName("ca")] long CreatedAt);
