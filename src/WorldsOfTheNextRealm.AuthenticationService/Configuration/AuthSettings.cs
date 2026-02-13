namespace WorldsOfTheNextRealm.AuthenticationService.Configuration;

public record AuthSettings
{
    public string MainTableName { get; init; } = "";
    public string CredentialsTableName { get; init; } = "";
    public string SigningKeysTableName { get; init; } = "";
    public string MasterEncryptionKey { get; init; } = "";
    public string MasterEncryptionKeySecretId { get; init; } = "";
    public int AccessTokenLifetimeSeconds { get; init; } = 21600; // 6 hours
    public long RefreshTokenLifetimeSeconds { get; init; } = 5_184_000; // 60 days
}
