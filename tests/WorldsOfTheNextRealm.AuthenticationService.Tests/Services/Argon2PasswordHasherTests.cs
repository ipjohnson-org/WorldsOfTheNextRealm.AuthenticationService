using DependencyModules.xUnit.Attributes;
using WorldsOfTheNextRealm.AuthenticationService.Services;

namespace WorldsOfTheNextRealm.AuthenticationService.Tests.Services;

public class Argon2PasswordHasherTests
{
    [ModuleTest]
    public async Task Hash_And_Verify_RoundTrip_Succeeds(IPasswordHasher hasher)
    {
        var password = "TestPassword123";

        var hash = await hasher.Hash(password);

        Assert.NotEmpty(hash);
        Assert.StartsWith("$argon2id$", hash);

        var result = await hasher.Verify(password, hash);
        Assert.True(result);
    }

    [ModuleTest]
    public async Task Verify_WrongPassword_ReturnsFalse(IPasswordHasher hasher)
    {
        var hash = await hasher.Hash("CorrectPassword1");

        var result = await hasher.Verify("WrongPassword1", hash);

        Assert.False(result);
    }

    [ModuleTest]
    public async Task Hash_ProducesDifferentHashesForSamePassword(IPasswordHasher hasher)
    {
        var password = "TestPassword123";

        var hash1 = await hasher.Hash(password);
        var hash2 = await hasher.Hash(password);

        Assert.NotEqual(hash1, hash2);
    }
}
