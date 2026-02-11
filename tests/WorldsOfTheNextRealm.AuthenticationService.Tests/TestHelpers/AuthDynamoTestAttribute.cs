using WorldsOfTheNextRealm.BackendCommon.Testing;

namespace WorldsOfTheNextRealm.AuthenticationService.Tests.TestHelpers;

public class AuthDynamoTestAttribute : DynamoTestAttribute
{
    public const string MainTable = "TestAuthMain";
    public const string CredentialsTable = "TestAuthCredentials";
    public const string SigningKeysTable = "TestAuthSigningKeys";

    protected override TableDefinition[] GetTableDefinitions()
    {
        return
        [
            new TableDefinition(MainTable, HasGsi1: true),
            new TableDefinition(CredentialsTable),
            new TableDefinition(SigningKeysTable, HasGsi1: true)
        ];
    }
}
