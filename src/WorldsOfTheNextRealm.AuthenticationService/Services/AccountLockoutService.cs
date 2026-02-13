using DependencyModules.Runtime.Attributes;
using Microsoft.Extensions.Logging;
using WorldsOfTheNextRealm.AuthenticationService.Entities;
using WorldsOfTheNextRealm.BackendCommon.DataStore;

namespace WorldsOfTheNextRealm.AuthenticationService.Services;

[SingletonService]
public class AccountLockoutService(IDataStore dataStore, IClock clock, ILogger<AccountLockoutService> logger) : IAccountLockoutService
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    public (bool IsLocked, DataDocument<PlayerCredentialsData>? UpdatedDoc) CheckLockout(
        DataDocument<PlayerCredentialsData> credDoc)
    {
        var creds = credDoc.Data;
        var nowMs = clock.UtcNow.ToUnixTimeMilliseconds();

        if (creds.Status == "locked" && creds.LockUntil > nowMs)
        {
            logger.LogDebug("Account locked for PlayerId={PlayerId} until={LockUntil}", creds.PlayerId, creds.LockUntil);
            return (true, null);
        }

        // Auto-unlock: if locked but lockUntil has passed, reset
        if (creds.Status == "locked" && creds.LockUntil <= nowMs)
        {
            logger.LogInformation("Account auto-unlocked for PlayerId={PlayerId}", creds.PlayerId);
            var resetData = creds with
            {
                Status = "active",
                FailedAttempts = 0,
                LockUntil = 0
            };
            var updatedDoc = credDoc with { Data = resetData };
            return (false, updatedDoc);
        }

        return (false, null);
    }

    public async Task<DataDocument<PlayerCredentialsData>> RecordFailedAttempt(
        DataDocument<PlayerCredentialsData> credDoc)
    {
        var creds = credDoc.Data;
        var newAttempts = creds.FailedAttempts + 1;
        var nowMs = clock.UtcNow.ToUnixTimeMilliseconds();

        logger.LogDebug("Failed login attempt {Count} for PlayerId={PlayerId}", newAttempts, creds.PlayerId);

        PlayerCredentialsData updatedData;

        if (newAttempts >= MaxFailedAttempts)
        {
            var lockUntilMs = clock.UtcNow.AddMinutes(LockoutMinutes).ToUnixTimeMilliseconds();
            updatedData = creds with
            {
                FailedAttempts = newAttempts,
                Status = "locked",
                LockUntil = lockUntilMs
            };
            logger.LogInformation("Account locked for PlayerId={PlayerId} after {Count} failed attempts", creds.PlayerId, newAttempts);
        }
        else
        {
            updatedData = creds with { FailedAttempts = newAttempts };
        }

        var updatedDoc = credDoc with { Data = updatedData };
        return await dataStore.Store(updatedDoc);
    }

    public async Task<DataDocument<PlayerCredentialsData>> ResetFailedAttempts(
        DataDocument<PlayerCredentialsData> credDoc)
    {
        var creds = credDoc.Data;

        if (creds.FailedAttempts == 0 && creds.Status == "active")
        {
            return credDoc;
        }

        logger.LogDebug("Resetting failed attempts for PlayerId={PlayerId}", creds.PlayerId);
        var resetData = creds with
        {
            FailedAttempts = 0,
            Status = "active",
            LockUntil = 0
        };

        var updatedDoc = credDoc with { Data = resetData };
        return await dataStore.Store(updatedDoc);
    }
}
