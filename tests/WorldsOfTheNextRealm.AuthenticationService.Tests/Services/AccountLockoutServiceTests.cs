using DependencyModules.xUnit.Attributes;
using NSubstitute;
using WorldsOfTheNextRealm.AuthenticationService.Entities;
using WorldsOfTheNextRealm.AuthenticationService.Services;
using WorldsOfTheNextRealm.AuthenticationService.Tests.TestHelpers;
using WorldsOfTheNextRealm.BackendCommon.DataStore;

namespace WorldsOfTheNextRealm.AuthenticationService.Tests.Services;

public class AccountLockoutServiceTests
{
    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public void CheckLockout_ActiveAccount_NotLocked(
        IAccountLockoutService lockoutService)
    {
        var creds = new PlayerCredentialsData("player1", "hash", "active", 0, 0, 1000);
        var doc = new DataDocument<PlayerCredentialsData>("table", "key", creds, 1);

        var (isLocked, _) = lockoutService.CheckLockout(doc);

        Assert.False(isLocked);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public void CheckLockout_LockedAccount_FutureLockUntil_ReturnsLocked(
        IAccountLockoutService lockoutService,
        IClock clock)
    {
        var futureMs = clock.UtcNow.AddMinutes(10).ToUnixTimeMilliseconds();
        var creds = new PlayerCredentialsData("player1", "hash", "locked", 5, futureMs, 1000);
        var doc = new DataDocument<PlayerCredentialsData>("table", "key", creds, 1);

        var (isLocked, _) = lockoutService.CheckLockout(doc);

        Assert.True(isLocked);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public void CheckLockout_LockedAccount_PastLockUntil_AutoUnlocks(
        IAccountLockoutService lockoutService,
        IClock clock)
    {
        var pastMs = clock.UtcNow.AddMinutes(-1).ToUnixTimeMilliseconds();
        var creds = new PlayerCredentialsData("player1", "hash", "locked", 5, pastMs, 1000);
        var doc = new DataDocument<PlayerCredentialsData>("table", "key", creds, 1);

        var (isLocked, updatedDoc) = lockoutService.CheckLockout(doc);

        Assert.False(isLocked);
        Assert.NotNull(updatedDoc);
        Assert.Equal("active", updatedDoc!.Data.Status);
        Assert.Equal(0, updatedDoc.Data.FailedAttempts);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task RecordFailedAttempt_IncrementsFails(
        IAccountLockoutService lockoutService)
    {
        var creds = new PlayerCredentialsData("player1", "hash", "active", 0, 0, 1000);
        var doc = new DataDocument<PlayerCredentialsData>(
            AuthDynamoTestAttribute.CredentialsTable,
            DataKeys.PlayerCredKey("player1"),
            creds, 0);

        var stored = await lockoutService.RecordFailedAttempt(doc);

        Assert.Equal(1, stored.Data.FailedAttempts);
        Assert.Equal("active", stored.Data.Status);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task RecordFailedAttempt_FifthFailure_LocksAccount(
        IAccountLockoutService lockoutService)
    {
        var creds = new PlayerCredentialsData("player1", "hash", "active", 4, 0, 1000);
        var doc = new DataDocument<PlayerCredentialsData>(
            AuthDynamoTestAttribute.CredentialsTable,
            DataKeys.PlayerCredKey("player1"),
            creds, 0);

        var stored = await lockoutService.RecordFailedAttempt(doc);

        Assert.Equal(5, stored.Data.FailedAttempts);
        Assert.Equal("locked", stored.Data.Status);
        Assert.True(stored.Data.LockUntil > 0);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task ResetFailedAttempts_ResetsToZero(
        IAccountLockoutService lockoutService)
    {
        var creds = new PlayerCredentialsData("player1", "hash", "active", 3, 0, 1000);
        var doc = new DataDocument<PlayerCredentialsData>(
            AuthDynamoTestAttribute.CredentialsTable,
            DataKeys.PlayerCredKey("player1"),
            creds, 0);

        var stored = await lockoutService.ResetFailedAttempts(doc);

        Assert.Equal(0, stored.Data.FailedAttempts);
        Assert.Equal("active", stored.Data.Status);
    }
}
