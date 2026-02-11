using System.Security.Cryptography;
using DependencyModules.xUnit.Attributes;
using Microsoft.AspNetCore.Http;
using WorldsOfTheNextRealm.AuthenticationService.Configuration;
using WorldsOfTheNextRealm.AuthenticationService.Entities;
using WorldsOfTheNextRealm.AuthenticationService.Services;
using WorldsOfTheNextRealm.AuthenticationService.Tests.TestHelpers;
using WorldsOfTheNextRealm.BackendCommon.DataStore;

namespace WorldsOfTheNextRealm.AuthenticationService.Tests.Endpoints;

public class JwksEndpointTests
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

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Jwks_ReturnsValidResponse(
        ISigningKeyService signingKeyService)
    {
        var httpContext = new DefaultHttpContext();

        var result = await AuthenticationService.Endpoints.JwksEndpoint.Handle(
            httpContext, signingKeyService);

        Assert.NotNull(result);
        Assert.Equal("public, max-age=3600", httpContext.Response.Headers.CacheControl.ToString());
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Jwks_KeyIsCached_ReturnsSameKey(
        IKeyEncryptionService keyEncryption,
        ISigningKeyService signingKeyService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        await SeedSigningKey(keyEncryption, dataStore, clock, settings);

        var jwks1 = await signingKeyService.GetJwks();
        var jwks2 = await signingKeyService.GetJwks();

        Assert.Equal(jwks1.Keys.Count, jwks2.Keys.Count);
        Assert.Equal(jwks1.Keys[0].Kid, jwks2.Keys[0].Kid);
    }
}
