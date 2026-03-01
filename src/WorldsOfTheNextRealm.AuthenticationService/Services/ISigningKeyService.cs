using Microsoft.IdentityModel.Tokens;

namespace WorldsOfTheNextRealm.AuthenticationService.Services;

public interface ISigningKeyService
{
    Task<(RsaSecurityKey Key, string Kid)> GetActiveSigningKey();
    Task<JsonWebKeySet> GetJwks();
}
