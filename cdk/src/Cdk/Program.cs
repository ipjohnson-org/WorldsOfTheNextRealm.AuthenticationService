using Amazon.CDK;
using WorldsOfTheNextRealm.BackendCommon.Cdk;

var app = new App();
var config = Config.ResolveEnvironment(app);

var masterKey = app.Node.TryGetContext("masterEncryptionKey")?.ToString()
    ?? throw new System.Exception("Missing required context: -c masterEncryptionKey=<base64-key>");

new LambdaApiStack(app, $"{config.Prefix}-AuthenticationService", new LambdaApiStackProps
{
    Env = config.Env,
    Prefix = config.Prefix,
    ServiceName = "WorldsOfTheNextRealm.AuthenticationService",
    PublishPath = "../src/WorldsOfTheNextRealm.AuthenticationService/bin/Release/net8.0/publish",
    Environment = new Dictionary<string, string>
    {
        ["AuthSettings__MasterEncryptionKey"] = masterKey
    }
});

app.Synth();