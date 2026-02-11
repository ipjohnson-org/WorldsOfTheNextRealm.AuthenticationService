namespace WorldsOfTheNextRealm.AuthenticationService.Services;

public interface IPasswordHasher
{
    Task<string> Hash(string password);
    Task<bool> Verify(string password, string hash);
}
