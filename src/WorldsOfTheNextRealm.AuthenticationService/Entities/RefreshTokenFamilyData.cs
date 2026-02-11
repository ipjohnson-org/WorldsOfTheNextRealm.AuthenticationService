namespace WorldsOfTheNextRealm.AuthenticationService.Entities;

public record RefreshTokenFamilyData(
    string FamilyId,
    string PlayerId,
    string CurrentTokenHash,
    int Sequence,
    string Status,
    long CreatedAt,
    long ExpiresAt);
