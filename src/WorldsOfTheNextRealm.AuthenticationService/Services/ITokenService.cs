using WorldsOfTheNextRealm.AuthenticationService.Models;

namespace WorldsOfTheNextRealm.AuthenticationService.Services;

public interface ITokenService
{
    Task<AuthResponse> CreateTokenPair(string playerId, string agent = "none");
    Task<string?> ValidateAccessToken(string token);
}
