using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Cryptography;
using DependencyModules.xUnit.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WorldsOfTheNextRealm.AuthenticationService.Configuration;
using WorldsOfTheNextRealm.AuthenticationService.Entities;
using WorldsOfTheNextRealm.AuthenticationService.Models;
using WorldsOfTheNextRealm.AuthenticationService.Services;
using WorldsOfTheNextRealm.AuthenticationService.Tests.TestHelpers;
using WorldsOfTheNextRealm.BackendCommon.DataStore;

namespace WorldsOfTheNextRealm.AuthenticationService.Tests.Endpoints;

public class RefreshEndpointTests
{
    private static async Task SeedSigningKey(
        IKeyEncryptionService keyEncryption,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        var rsa = RSA.Create(2048);
        var kid = $"key-{clock.UtcNow:yyyy-MM-dd}";
        var encryptedPrivateKey = keyEncryption.Encrypt(rsa.ExportRSAPrivateKey());
        var publicKeyBase64 = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
        var nowMs = clock.UtcNow.ToUnixTimeMilliseconds();

        var keyData = new SigningKeyData(
            Kid: kid, Algorithm: "RS256", PublicKey: publicKeyBase64,
            EncryptedPrivateKey: encryptedPrivateKey, Status: "active",
            CreatedAt: nowMs, RotatedAt: 0, RetiredAt: 0);

        var doc = new DataDocument<SigningKeyData>(
            settings.SigningKeysTableName,
            DataKeys.SigningKeyKey(kid),
            keyData, 0,
            Gsi1Pk: DataKeys.SigningKeysGsi1Pk,
            Gsi1Sk: nowMs.ToString("D20"));

        await dataStore.Store(doc);
    }

    private static async Task<AuthResponse> RegisterAndGetTokens(
        IPasswordHasher passwordHasher,
        IKeyEncryptionService keyEncryption,
        ITokenService tokenService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings,
        string email = "refresh@example.com")
    {
        await SeedSigningKey(keyEncryption, dataStore, clock, settings);

        var playerId = Guid.NewGuid().ToString();
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var passwordHash = await passwordHasher.Hash("StrongPass1");
        var nowMs = clock.UtcNow.ToUnixTimeMilliseconds();

        var emailDoc = new DataDocument<EmailLookupData>(
            settings.MainTableName,
            DataKeys.EmailKey(normalizedEmail),
            new EmailLookupData(normalizedEmail, playerId, nowMs),
            0);

        var credDoc = new DataDocument<PlayerCredentialsData>(
            settings.CredentialsTableName,
            DataKeys.PlayerCredKey(playerId),
            new PlayerCredentialsData(playerId, passwordHash, "active", 0, 0, nowMs),
            0);

        await dataStore.TransactStore(emailDoc, credDoc);

        return await tokenService.CreateTokenPair(playerId);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Refresh_HappyPath_RotatesTokens(
        IPasswordHasher passwordHasher,
        IKeyEncryptionService keyEncryption,
        ITokenService tokenService,
        ISigningKeyService signingKeyService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        var tokens = await RegisterAndGetTokens(
            passwordHasher, keyEncryption,
            tokenService, dataStore, clock, settings);

        var result = await AuthenticationService.Endpoints.RefreshEndpoint.Handle(
            new RefreshRequest(tokens.RefreshToken),
            signingKeyService, dataStore, clock, settings, new NullLoggerFactory());

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(200, httpResult.StatusCode);

        var valueResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
        var newTokens = Assert.IsType<AuthResponse>(valueResult.Value);
        Assert.NotEqual(tokens.RefreshToken, newTokens.RefreshToken);
        Assert.NotEmpty(newTokens.AccessToken);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Refresh_ReplayDetection_RevokesFamily(
        IPasswordHasher passwordHasher,
        IKeyEncryptionService keyEncryption,
        ITokenService tokenService,
        ISigningKeyService signingKeyService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        var tokens = await RegisterAndGetTokens(
            passwordHasher, keyEncryption,
            tokenService, dataStore, clock, settings,
            "replay@example.com");

        var originalRefreshToken = tokens.RefreshToken;

        // Use the refresh token once (valid)
        await AuthenticationService.Endpoints.RefreshEndpoint.Handle(
            new RefreshRequest(originalRefreshToken),
            signingKeyService, dataStore, clock, settings, new NullLoggerFactory());

        // Replay the same token (should detect theft)
        var replayResult = await AuthenticationService.Endpoints.RefreshEndpoint.Handle(
            new RefreshRequest(originalRefreshToken),
            signingKeyService, dataStore, clock, settings, new NullLoggerFactory());

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(replayResult);
        Assert.Equal(401, httpResult.StatusCode);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Refresh_HappyPath_PreservesAgentAndSidClaims(
        IPasswordHasher passwordHasher,
        IKeyEncryptionService keyEncryption,
        ITokenService tokenService,
        ISigningKeyService signingKeyService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        var tokens = await RegisterAndGetTokens(
            passwordHasher, keyEncryption,
            tokenService, dataStore, clock, settings,
            "claimspreserve@example.com");

        // Parse the original access token to get the session ID
        var handler = new JsonWebTokenHandler();
        var originalJwt = handler.ReadJsonWebToken(tokens.AccessToken);
        var originalSid = originalJwt.Claims.First(c => c.Type == "sid").Value;

        var result = await AuthenticationService.Endpoints.RefreshEndpoint.Handle(
            new RefreshRequest(tokens.RefreshToken),
            signingKeyService, dataStore, clock, settings, new NullLoggerFactory());

        var valueResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
        var newTokens = Assert.IsType<AuthResponse>(valueResult.Value);

        var refreshedJwt = handler.ReadJsonWebToken(newTokens.AccessToken);
        Assert.Equal("none", refreshedJwt.Claims.First(c => c.Type == "agent").Value);
        Assert.Equal(originalSid, refreshedJwt.Claims.First(c => c.Type == "sid").Value);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Refresh_InvalidToken_Returns401(
        ISigningKeyService signingKeyService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        var result = await AuthenticationService.Endpoints.RefreshEndpoint.Handle(
            new RefreshRequest("nonexistent-family-id.invalidtoken"),
            signingKeyService, dataStore, clock, settings, new NullLoggerFactory());

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(401, httpResult.StatusCode);
    }
}
