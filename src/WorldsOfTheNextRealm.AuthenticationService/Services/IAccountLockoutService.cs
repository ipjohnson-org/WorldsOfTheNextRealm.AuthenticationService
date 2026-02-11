using WorldsOfTheNextRealm.BackendCommon.DataStore;
using WorldsOfTheNextRealm.AuthenticationService.Entities;

namespace WorldsOfTheNextRealm.AuthenticationService.Services;

public interface IAccountLockoutService
{
    (bool IsLocked, DataDocument<PlayerCredentialsData>? UpdatedDoc) CheckLockout(
        DataDocument<PlayerCredentialsData> credDoc);

    Task<DataDocument<PlayerCredentialsData>> RecordFailedAttempt(
        DataDocument<PlayerCredentialsData> credDoc);

    Task<DataDocument<PlayerCredentialsData>> ResetFailedAttempts(
        DataDocument<PlayerCredentialsData> credDoc);
}
