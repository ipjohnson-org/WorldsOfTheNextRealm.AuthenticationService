using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using WorldsOfTheNextRealm.AuthenticationService.Configuration;
using WorldsOfTheNextRealm.AuthenticationService.Entities;
using WorldsOfTheNextRealm.AuthenticationService.Models;
using WorldsOfTheNextRealm.AuthenticationService.Services;
using WorldsOfTheNextRealm.BackendCommon.DataStore;

namespace WorldsOfTheNextRealm.AuthenticationService.Endpoints;

public static class RefreshEndpoint
{
    public static async Task<IResult> Handle(
        RefreshRequest request,
        ISigningKeyService signingKeyService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(typeof(RefreshEndpoint).FullName!);
        var invalidTokenResponse = ErrorResponse.Create("invalid_token", "The refresh token is invalid or expired.");

        // Parse refresh token: {familyId}.{randomPart}
        var dotIndex = request.RefreshToken?.IndexOf('.');
        if (dotIndex is null or < 1)
        {
            logger.LogDebug("Refresh failed: invalid token format");
            return Results.Json(invalidTokenResponse, statusCode: 401);
        }

        var familyId = request.RefreshToken![..dotIndex.Value];
        var tokenHash = TokenService.ComputeSha256(request.RefreshToken);

        logger.LogDebug("Refresh attempt for familyId={FamilyId}", familyId);

        // Get family
        var familyDoc = await dataStore.Get<RefreshTokenFamilyData>(
            settings.MainTableName, DataKeys.RefreshFamilyKey(familyId));

        if (familyDoc is null)
        {
            logger.LogDebug("Refresh failed: family not found for familyId={FamilyId}", familyId);
            return Results.Json(invalidTokenResponse, statusCode: 401);
        }

        var family = familyDoc.Data;
        var nowMs = clock.UtcNow.ToUnixTimeMilliseconds();

        // Check if revoked or expired
        if (family.Status == "revoked" || family.ExpiresAt <= nowMs)
        {
            logger.LogDebug("Refresh failed: token expired or revoked for familyId={FamilyId}", familyId);
            return Results.Json(invalidTokenResponse, statusCode: 401);
        }

        // Check token hash — replay detection
        if (family.CurrentTokenHash != tokenHash)
        {
            // Replay detected — revoke the entire family
            logger.LogWarning("Token replay detected for familyId={FamilyId} PlayerId={PlayerId}", familyId, family.PlayerId);
            var revokedFamily = family with { Status = "revoked" };
            var revokedDoc = familyDoc with { Data = revokedFamily };
            await dataStore.Store(revokedDoc);

            return Results.Json(
                ErrorResponse.Create("token_revoked", "Refresh token reuse detected. Family has been revoked."),
                statusCode: 401);
        }

        // Generate new refresh token in same family
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var randomPart = Base64UrlEncoder.Encode(randomBytes);
        var newRefreshToken = $"{familyId}.{randomPart}";
        var newTokenHash = TokenService.ComputeSha256(newRefreshToken);

        // Update family
        var updatedFamily = family with
        {
            CurrentTokenHash = newTokenHash,
            Sequence = family.Sequence + 1
        };
        var updatedDoc = familyDoc with { Data = updatedFamily };
        await dataStore.Store(updatedDoc);

        // Generate new access token
        var (signingKey, kid) = await signingKeyService.GetActiveSigningKey();
        var now = clock.UtcNow;

        var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);
        var descriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim("sub", family.PlayerId),
                new System.Security.Claims.Claim("jti", Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim("agent", family.Agent),
                new System.Security.Claims.Claim("sid", family.SessionId ?? ""),
            ]),
            IssuedAt = now.UtcDateTime,
            Expires = now.AddSeconds(settings.AccessTokenLifetimeSeconds).UtcDateTime,
            SigningCredentials = credentials,
        };

        var accessToken = handler.CreateToken(descriptor);

        logger.LogDebug("Refresh successful for familyId={FamilyId} sequence={Sequence}", familyId, updatedFamily.Sequence);
        return Results.Ok(new AuthResponse(accessToken, newRefreshToken, settings.AccessTokenLifetimeSeconds));
    }
}
