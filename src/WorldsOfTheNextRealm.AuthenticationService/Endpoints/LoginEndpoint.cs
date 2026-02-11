using WorldsOfTheNextRealm.AuthenticationService.Configuration;
using WorldsOfTheNextRealm.AuthenticationService.Entities;
using WorldsOfTheNextRealm.AuthenticationService.Models;
using WorldsOfTheNextRealm.AuthenticationService.Services;
using WorldsOfTheNextRealm.BackendCommon.DataStore;

namespace WorldsOfTheNextRealm.AuthenticationService.Endpoints;

public static class LoginEndpoint
{
    public static async Task<IResult> Handle(
        AuthRequest request,
        IEmailValidator emailValidator,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IAccountLockoutService lockoutService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        var invalidCredentialsResponse = ErrorResponse.Create("invalid_credentials", "The email or password is incorrect.");

        // Normalize email
        var (emailValid, normalizedEmail, _) = emailValidator.Validate(request.Email);
        if (!emailValid)
        {
            // Still hash dummy to maintain constant time
            await passwordHasher.Verify("dummy", "$argon2id$v=19$m=65536,t=3,p=1$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
            return Results.Unauthorized();
        }

        // Look up email
        var emailDoc = await dataStore.Get<EmailLookupData>(
            settings.MainTableName, DataKeys.EmailKey(normalizedEmail!));

        if (emailDoc is null)
        {
            // Constant-time: hash dummy password
            await passwordHasher.Verify("dummy", "$argon2id$v=19$m=65536,t=3,p=1$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
            return Results.Json(invalidCredentialsResponse, statusCode: 401);
        }

        var playerId = emailDoc.Data.PlayerId;

        // Get credentials
        var credDoc = await dataStore.Get<PlayerCredentialsData>(
            settings.CredentialsTableName, DataKeys.PlayerCredKey(playerId));

        if (credDoc is null)
        {
            return Results.Json(invalidCredentialsResponse, statusCode: 401);
        }

        // Check lockout
        var (isLocked, updatedDoc) = lockoutService.CheckLockout(credDoc);
        if (isLocked)
        {
            return Results.Json(ErrorResponse.Create("account_locked", "Account is temporarily locked. Try again later."), statusCode: 403);
        }

        // If lockout was auto-reset, use the updated doc and persist it
        if (updatedDoc is not null)
        {
            credDoc = await dataStore.Store(updatedDoc);
        }

        // Verify password
        var passwordValid = await passwordHasher.Verify(request.Password, credDoc.Data.PasswordHash);
        if (!passwordValid)
        {
            await lockoutService.RecordFailedAttempt(credDoc);
            return Results.Json(invalidCredentialsResponse, statusCode: 401);
        }

        // Reset failed attempts on success
        await lockoutService.ResetFailedAttempts(credDoc);

        // Create token pair
        var authResponse = await tokenService.CreateTokenPair(playerId);
        return Results.Ok(authResponse);
    }
}
