using WorldsOfTheNextRealm.BackendCommon.DataStore;

namespace WorldsOfTheNextRealm.AuthenticationService;

public static class DataKeys
{
    public static string EmailKey(string normalizedEmail) =>
        DataKeyBuilder.BuildKey("email", normalizedEmail);

    public static string PlayerCredKey(string playerId) =>
        DataKeyBuilder.BuildKey("player-cred", playerId);

    public static string RefreshFamilyKey(string familyId) =>
        DataKeyBuilder.BuildKey("refresh-family", familyId);

    public static string SigningKeyKey(string kid) =>
        DataKeyBuilder.BuildKey("signing-key", kid);

    public static string PlayerRefreshGsi1Pk(string playerId) =>
        $"player-refresh/{playerId}";

    public const string SigningKeysGsi1Pk = "signing-keys";
}
