using DependencyModules.Runtime.Attributes;
using WorldsOfTheNextRealm.AuthenticationService.Entities;
using WorldsOfTheNextRealm.BackendCommon.DataStore;

namespace WorldsOfTheNextRealm.AuthenticationService.Services;

[SingletonService]
public class AccountLockoutService(IDataStore dataStore, IClock clock) : IAccountLockoutService
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
            return (true, null);
        }

        // Auto-unlock: if locked but lockUntil has passed, reset
        if (creds.Status == "locked" && creds.LockUntil <= nowMs)
        {
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
