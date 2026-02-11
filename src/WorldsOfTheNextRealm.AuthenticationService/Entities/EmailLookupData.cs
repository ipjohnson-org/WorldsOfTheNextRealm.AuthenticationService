namespace WorldsOfTheNextRealm.AuthenticationService.Entities;

public record EmailLookupData(
    string NormalizedEmail,
    string PlayerId,
    long CreatedAt);
