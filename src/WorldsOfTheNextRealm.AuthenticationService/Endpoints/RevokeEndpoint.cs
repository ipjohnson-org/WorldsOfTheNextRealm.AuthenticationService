using Microsoft.Extensions.Logging;
using WorldsOfTheNextRealm.AuthenticationService.Configuration;
using WorldsOfTheNextRealm.AuthenticationService.Entities;
using WorldsOfTheNextRealm.AuthenticationService.Models;
using WorldsOfTheNextRealm.AuthenticationService.Services;
using WorldsOfTheNextRealm.BackendCommon.DataStore;

namespace WorldsOfTheNextRealm.AuthenticationService.Endpoints;

public static class RevokeEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext httpContext,
        ITokenService tokenService,
        IDataStore dataStore,
        AuthSettings settings,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(typeof(RevokeEndpoint).FullName!);

        // Validate access token from Authorization header
        var authHeader = httpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Revoke failed: missing or invalid authorization header");
            return Results.Json(
                ErrorResponse.Create("invalid_access_token", "Missing or invalid access token."),
                statusCode: 401);
        }

        var accessToken = authHeader["Bearer ".Length..];
        var playerId = await tokenService.ValidateAccessToken(accessToken);
        if (playerId is null)
        {
            return Results.Json(
                ErrorResponse.Create("invalid_access_token", "Missing or invalid access token."),
                statusCode: 401);
        }

        // Read body
        var request = await httpContext.Request.ReadFromJsonAsync<RevokeRequest>();
        if (request?.RefreshToken is null or "")
        {
            return Results.Json(
                ErrorResponse.Create("invalid_request", "Missing refresh token in request body."),
                statusCode: 400);
        }

        // Parse refresh token to get familyId
        var dotIndex = request.RefreshToken.IndexOf('.');
        if (dotIndex < 1)
        {
            return Results.Json(
                ErrorResponse.Create("invalid_request", "Invalid refresh token format."),
                statusCode: 400);
        }

        var familyId = request.RefreshToken[..dotIndex];

        // Get family and revoke
        var familyDoc = await dataStore.Get<RefreshTokenFamilyData>(
            settings.MainTableName, DataKeys.RefreshFamilyKey(familyId));

        if (familyDoc is not null)
        {
            var revokedFamily = familyDoc.Data with { Status = "revoked" };
            var revokedDoc = familyDoc with { Data = revokedFamily };
            await dataStore.Store(revokedDoc);
            logger.LogInformation("Token family revoked familyId={FamilyId} PlayerId={PlayerId}", familyId, playerId);
        }
        else
        {
            logger.LogDebug("Revoke: family not found for familyId={FamilyId}", familyId);
        }

        return Results.NoContent();
    }
}
