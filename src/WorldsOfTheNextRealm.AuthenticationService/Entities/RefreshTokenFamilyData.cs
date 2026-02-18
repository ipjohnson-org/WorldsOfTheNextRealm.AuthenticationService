using System.Text.Json.Serialization;

namespace WorldsOfTheNextRealm.AuthenticationService.Entities;

public record RefreshTokenFamilyData(
    [property: JsonPropertyName("fid")] string FamilyId,
    [property: JsonPropertyName("pid")] string PlayerId,
    [property: JsonPropertyName("cth")] string CurrentTokenHash,
    [property: JsonPropertyName("seq")] int Sequence,
    [property: JsonPropertyName("st")] string Status,
    [property: JsonPropertyName("ca")] long CreatedAt,
    [property: JsonPropertyName("ea")] long ExpiresAt,
    [property: JsonPropertyName("ag")] string Agent = "none",
    [property: JsonPropertyName("sid")] string? SessionId = null);
