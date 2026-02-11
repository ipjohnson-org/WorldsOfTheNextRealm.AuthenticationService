using System.Security.Cryptography;
using DependencyModules.xUnit.Attributes;
using Microsoft.AspNetCore.Http;
using WorldsOfTheNextRealm.AuthenticationService.Configuration;
using WorldsOfTheNextRealm.AuthenticationService.Entities;
using WorldsOfTheNextRealm.AuthenticationService.Models;
using WorldsOfTheNextRealm.AuthenticationService.Services;
using WorldsOfTheNextRealm.AuthenticationService.Tests.TestHelpers;
using WorldsOfTheNextRealm.BackendCommon.DataStore;

namespace WorldsOfTheNextRealm.AuthenticationService.Tests.Endpoints;

public class RevokeEndpointTests
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
        string email = "revoke@example.com")
    {
        // Seed signing key so the JWKS cache is populated correctly
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

    private static HttpContext CreateHttpContext(string? accessToken, RevokeRequest? body)
    {
        var context = new DefaultHttpContext();

        if (accessToken is not null)
        {
            context.Request.Headers.Authorization = $"Bearer {accessToken}";
        }

        if (body is not null)
        {
            var json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(body);
            context.Request.Body = new MemoryStream(json);
            context.Request.ContentType = "application/json";
        }

        return context;
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Revoke_HappyPath_Returns204(
        IPasswordHasher passwordHasher,
        IKeyEncryptionService keyEncryption,
        ITokenService tokenService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        var tokens = await RegisterAndGetTokens(
            passwordHasher, keyEncryption,
            tokenService, dataStore, clock, settings);

        var httpContext = CreateHttpContext(tokens.AccessToken, new RevokeRequest(tokens.RefreshToken));

        var result = await AuthenticationService.Endpoints.RevokeEndpoint.Handle(
            httpContext, tokenService, dataStore, settings);

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(204, httpResult.StatusCode);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Revoke_InvalidAccessToken_Returns401(
        ITokenService tokenService,
        IDataStore dataStore,
        AuthSettings settings)
    {
        var httpContext = CreateHttpContext("invalid-token", new RevokeRequest("some.refresh"));

        var result = await AuthenticationService.Endpoints.RevokeEndpoint.Handle(
            httpContext, tokenService, dataStore, settings);

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(401, httpResult.StatusCode);
    }
}
