using System.Security.Cryptography;
using DependencyModules.Runtime.Attributes;
using Microsoft.IdentityModel.Tokens;
using WorldsOfTheNextRealm.AuthenticationService.Configuration;
using WorldsOfTheNextRealm.AuthenticationService.Entities;
using WorldsOfTheNextRealm.BackendCommon.DataStore;

namespace WorldsOfTheNextRealm.AuthenticationService.Services;

[SingletonService]
public class SigningKeyService(
    IDataStore dataStore,
    IKeyEncryptionService keyEncryption,
    IClock clock,
    AuthSettings settings) : ISigningKeyService
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private RsaSecurityKey? _cachedKey;
    private string? _cachedKid;
    private JsonWebKeySet? _cachedJwks;

    public async Task<(RsaSecurityKey Key, string Kid)> GetActiveSigningKey()
    {
        if (_cachedKey is not null && _cachedKid is not null)
        {
            return (_cachedKey, _cachedKid);
        }

        await _lock.WaitAsync();
        try
        {
            if (_cachedKey is not null && _cachedKid is not null)
            {
                return (_cachedKey, _cachedKid);
            }

            await EnsureActiveKey();
            return (_cachedKey!, _cachedKid!);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<JsonWebKeySet> GetJwks()
    {
        if (_cachedJwks is not null)
        {
            return _cachedJwks;
        }

        await _lock.WaitAsync();
        try
        {
            if (_cachedJwks is not null)
            {
                return _cachedJwks;
            }

            await EnsureActiveKey();
            return _cachedJwks!;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task EnsureActiveKey()
    {
        // Query all signing keys from GSI1
        var result = await dataStore.QueryGsi1<SigningKeyData>(
            settings.SigningKeysTableName,
            DataKeys.SigningKeysGsi1Pk,
            scanForward: false,
            limit: 10);

        DataDocument<SigningKeyData>? activeDoc = null;

        foreach (var doc in result.Items)
        {
            if (doc.Data.Status == "active")
            {
                activeDoc = doc;
                break;
            }
        }

        if (activeDoc is null)
        {
            activeDoc = await BootstrapSigningKey();
        }

        var keyData = activeDoc.Data;
        var privateKeyBytes = keyEncryption.Decrypt(keyData.EncryptedPrivateKey);
        var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKeyBytes, out _);

        _cachedKey = new RsaSecurityKey(rsa) { KeyId = keyData.Kid };
        _cachedKid = keyData.Kid;

        // Build JWKS from all non-retired-removed keys
        var jwks = new JsonWebKeySet();
        foreach (var doc in result.Items)
        {
            var data = doc.Data;
            if (data.Status is "active" or "rotated" or "retired")
            {
                var pubRsa = RSA.Create();
                pubRsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(data.PublicKey), out _);
                var pubKey = new RsaSecurityKey(pubRsa) { KeyId = data.Kid };
                var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(pubKey);
                jwk.Use = "sig";
                jwk.Alg = "RS256";
                jwks.Keys.Add(jwk);
            }
        }

        _cachedJwks = jwks;
    }

    private async Task<DataDocument<SigningKeyData>> BootstrapSigningKey()
    {
        var rsa = RSA.Create(2048);
        var kid = $"key-{clock.UtcNow:yyyy-MM-dd}";

        var privateKeyBytes = rsa.ExportRSAPrivateKey();
        var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();

        var encryptedPrivateKey = keyEncryption.Encrypt(privateKeyBytes);
        var publicKeyBase64 = Convert.ToBase64String(publicKeyBytes);

        var nowMs = clock.UtcNow.ToUnixTimeMilliseconds();
        var keyData = new SigningKeyData(
            Kid: kid,
            Algorithm: "RS256",
            PublicKey: publicKeyBase64,
            EncryptedPrivateKey: encryptedPrivateKey,
            Status: "active",
            CreatedAt: nowMs,
            RotatedAt: 0,
            RetiredAt: 0);

        var doc = new DataDocument<SigningKeyData>(
            settings.SigningKeysTableName,
            DataKeys.SigningKeyKey(kid),
            keyData,
            0,
            Gsi1Pk: DataKeys.SigningKeysGsi1Pk,
            Gsi1Sk: nowMs.ToString("D20"));

        return await dataStore.Store(doc);
    }
}
