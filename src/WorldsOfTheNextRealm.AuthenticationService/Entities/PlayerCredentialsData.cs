namespace WorldsOfTheNextRealm.AuthenticationService.Entities;

public record PlayerCredentialsData(
    string PlayerId,
    string PasswordHash,
    string Status,
    int FailedAttempts,
    long LockUntil,
    long CreatedAt);
