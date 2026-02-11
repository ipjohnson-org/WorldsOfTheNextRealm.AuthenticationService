using Amazon.DynamoDBv2.Model;
using WorldsOfTheNextRealm.AuthenticationService.Configuration;
using WorldsOfTheNextRealm.AuthenticationService.Entities;
using WorldsOfTheNextRealm.AuthenticationService.Models;
using WorldsOfTheNextRealm.AuthenticationService.Services;
using WorldsOfTheNextRealm.BackendCommon.DataStore;

namespace WorldsOfTheNextRealm.AuthenticationService.Endpoints;

public static class RegisterEndpoint
{
    public static async Task<IResult> Handle(
        AuthRequest request,
        IEmailValidator emailValidator,
        IPasswordValidator passwordValidator,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        // Validate email
        var (emailValid, normalizedEmail, emailError) = emailValidator.Validate(request.Email);
        if (!emailValid)
        {
            return Results.UnprocessableEntity(ErrorResponse.Create(emailError!, "Email format is invalid."));
        }

        // Validate password
        var (passwordValid, passwordError) = passwordValidator.Validate(request.Password);
        if (!passwordValid)
        {
            return Results.UnprocessableEntity(ErrorResponse.Create(passwordError!, "Password does not meet requirements."));
        }

        // Generate player ID and hash password
        var playerId = Guid.NewGuid().ToString();
        var passwordHash = await passwordHasher.Hash(request.Password);
        var nowMs = clock.UtcNow.ToUnixTimeMilliseconds();

        // Build email lookup document
        var emailData = new EmailLookupData(normalizedEmail!, playerId, nowMs);
        var emailDoc = new DataDocument<EmailLookupData>(
            settings.MainTableName,
            DataKeys.EmailKey(normalizedEmail!),
            emailData,
            0);

        // Build credentials document
        var credData = new PlayerCredentialsData(playerId, passwordHash, "active", 0, 0, nowMs);
        var credDoc = new DataDocument<PlayerCredentialsData>(
            settings.CredentialsTableName,
            DataKeys.PlayerCredKey(playerId),
            credData,
            0);

        // Atomic transaction — fails if email PK already exists (versionId 0 = new doc condition)
        try
        {
            await dataStore.TransactStore(emailDoc, credDoc);
        }
        catch (TransactionCanceledException)
        {
            return Results.Conflict(ErrorResponse.Create("email_taken", "This email is already registered."));
        }

        // Create token pair
        var authResponse = await tokenService.CreateTokenPair(playerId);

        return Results.Created("/auth/register", authResponse);
    }
}
