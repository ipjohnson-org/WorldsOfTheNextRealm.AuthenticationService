namespace WorldsOfTheNextRealm.AuthenticationService.Services;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
