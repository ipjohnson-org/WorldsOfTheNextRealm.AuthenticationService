using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.SecretsManager;
using WorldsOfTheNextRealm.BackendCommon.Cdk;

var app = new App();
var config = Config.ResolveEnvironment(app);

var stack = new LambdaApiStack(app, $"{config.Prefix}-AuthenticationService", new LambdaApiStackProps
{
    Env = config.Env,
    Prefix = config.Prefix,
    ServiceName = "WorldsOfTheNextRealm.AuthenticationService",
    PublishPath = "../src/WorldsOfTheNextRealm.AuthenticationService/bin/Release/net8.0/publish",
    Environment = new Dictionary<string, string>
    {
        ["AuthSettings__MasterEncryptionKeySecretId"] = $"{config.Prefix}/auth/master-encryption-key"
    }
});

var secret = new Secret(stack, "MasterEncryptionKey", new SecretProps
{
    SecretName = $"{config.Prefix}/auth/master-encryption-key",
    Description = "AES-256 master encryption key for signing key encryption"
});
secret.GrantRead(stack.Function);

var authMain = Table.FromTableName(stack, "AuthMain", "AuthMain");
var authCredentials = Table.FromTableName(stack, "AuthCredentials", "AuthCredentials");
var authSigningKeys = Table.FromTableName(stack, "AuthSigningKeys", "AuthSigningKeys");

authMain.GrantReadWriteData(stack.Function);
authCredentials.GrantReadWriteData(stack.Function);
authSigningKeys.GrantReadWriteData(stack.Function);

app.Synth();