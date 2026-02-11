using System.Security.Cryptography;
using DependencyModules.Runtime.Attributes;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WorldsOfTheNextRealm.AuthenticationService.Configuration;
using WorldsOfTheNextRealm.AuthenticationService.Entities;
using WorldsOfTheNextRealm.AuthenticationService.Models;
using WorldsOfTheNextRealm.BackendCommon.DataStore;

namespace WorldsOfTheNextRealm.AuthenticationService.Services;

[SingletonService]
public class TokenService(
    ISigningKeyService signingKeyService,
    IDataStore dataStore,
    IClock clock,
    AuthSettings settings) : ITokenService
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    public async Task<AuthResponse> CreateTokenPair(string playerId)
    {
        var (signingKey, kid) = await signingKeyService.GetActiveSigningKey();
        var now = clock.UtcNow;

        // Create access token
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim("sub", playerId),
                new System.Security.Claims.Claim("jti", Guid.NewGuid().ToString()),
            ]),
            IssuedAt = now.UtcDateTime,
            Expires = now.AddSeconds(settings.AccessTokenLifetimeSeconds).UtcDateTime,
            SigningCredentials = credentials,
        };

        var accessToken = TokenHandler.CreateToken(descriptor);

        // Create refresh token
        var familyId = Guid.NewGuid().ToString();
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var randomPart = Base64UrlEncoder.Encode(randomBytes);
        var refreshToken = $"{familyId}.{randomPart}";

        // Store refresh family
        var tokenHash = ComputeSha256(refreshToken);
        var nowMs = now.ToUnixTimeMilliseconds();
        var expiresAtMs = now.AddSeconds(settings.RefreshTokenLifetimeSeconds).ToUnixTimeMilliseconds();

        var familyData = new RefreshTokenFamilyData(
            FamilyId: familyId,
            PlayerId: playerId,
            CurrentTokenHash: tokenHash,
            Sequence: 1,
            Status: "active",
            CreatedAt: nowMs,
            ExpiresAt: expiresAtMs);

        var familyDoc = new DataDocument<RefreshTokenFamilyData>(
            settings.MainTableName,
            DataKeys.RefreshFamilyKey(familyId),
            familyData,
            0,
            Gsi1Pk: DataKeys.PlayerRefreshGsi1Pk(playerId),
            Gsi1Sk: nowMs.ToString("D20"),
            Ttl: expiresAtMs / 1000);

        await dataStore.Store(familyDoc);

        return new AuthResponse(accessToken, refreshToken, settings.AccessTokenLifetimeSeconds);
    }

    public async Task<string?> ValidateAccessToken(string token)
    {
        var jwks = await signingKeyService.GetJwks();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKeys = jwks.Keys.Select(k =>
            {
                if (k.Kty == "RSA")
                {
                    return new RsaSecurityKey(new RSAParameters
                    {
                        Modulus = Base64UrlEncoder.DecodeBytes(k.N),
                        Exponent = Base64UrlEncoder.DecodeBytes(k.E)
                    }) { KeyId = k.Kid };
                }
                return (SecurityKey?)null;
            }).Where(k => k is not null).Cast<SecurityKey>(),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        var result = await TokenHandler.ValidateTokenAsync(token, validationParameters);

        if (!result.IsValid)
        {
            return null;
        }

        return result.Claims.TryGetValue("sub", out var sub) ? sub?.ToString() : null;
    }

    public static string ComputeSha256(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
