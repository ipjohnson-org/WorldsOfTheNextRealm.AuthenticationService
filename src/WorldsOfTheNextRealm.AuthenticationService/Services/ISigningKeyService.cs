using Microsoft.IdentityModel.Tokens;
using WorldsOfTheNextRealm.AuthenticationService.Entities;
using WorldsOfTheNextRealm.BackendCommon.DataStore;

namespace WorldsOfTheNextRealm.AuthenticationService.Services;

public interface ISigningKeyService
{
    Task<(RsaSecurityKey Key, string Kid)> GetActiveSigningKey();
    Task<JsonWebKeySet> GetJwks();
}
