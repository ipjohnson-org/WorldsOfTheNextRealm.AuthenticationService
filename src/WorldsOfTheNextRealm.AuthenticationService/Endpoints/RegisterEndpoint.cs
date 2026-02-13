using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using WorldsOfTheNextRealm.AuthenticationService.Configuration;
using WorldsOfTheNextRealm.AuthenticationService.Entities;
using WorldsOfTheNextRealm.AuthenticationService.Logging;
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
        AuthSettings settings,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(typeof(RegisterEndpoint).FullName!);
        logger.LogDebug("Registration attempt for email={MaskedEmail}", LogSanitizer.MaskEmail(request.Email));

        // Validate email
        var (emailValid, normalizedEmail, emailError) = emailValidator.Validate(request.Email);
        if (!emailValid)
        {
            logger.LogDebug("Registration failed: invalid email format");
            return Results.UnprocessableEntity(ErrorResponse.Create(emailError!, "Email format is invalid."));
        }

        // Validate password
        var (passwordValid, passwordError) = passwordValidator.Validate(request.Password);
        if (!passwordValid)
        {
            logger.LogDebug("Registration failed: password validation error={Error}", passwordError);
            return Results.UnprocessableEntity(ErrorResponse.Create(passwordError!, "Password does not meet requirements."));
        }

        // Generate player ID and hash password
        var playerId = Guid.NewGuid().ToString();
        logger.LogDebug("Generated PlayerId={PlayerId} for registration", playerId);
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
            logger.LogDebug("Registration failed: duplicate email for PlayerId={PlayerId}", playerId);
            return Results.Conflict(ErrorResponse.Create("email_taken", "This email is already registered."));
        }

        // Create token pair
        var authResponse = await tokenService.CreateTokenPair(playerId);

        logger.LogInformation("Registration successful for PlayerId={PlayerId}", playerId);
        return Results.Created("/auth/register", authResponse);
    }
}
